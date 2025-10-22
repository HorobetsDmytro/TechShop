using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechShop.Interfaces;
using TechShop.Models;
using TechShop.Services;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Font = iTextSharp.text.Font;
using Rectangle = iTextSharp.text.Rectangle;

namespace TechShop.Areas.Admin.Controllers;

[Authorize(Roles = "Admin")]
[Area("Admin")]
public class OrderController : Controller
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEmailService _emailService;
    private readonly IProductRepository _productRepository;

    public OrderController(IOrderRepository orderRepository, IEmailService emailService, IProductRepository productRepository)
    {
        _orderRepository = orderRepository;
        _emailService = emailService;
        _productRepository = productRepository;
    }

    public async Task<IActionResult> Index(OrderStatus? status, int page = 1)
    {
        const int pageSize = 10;
        
        var allOrders = await _orderRepository.GetAllAsync();

        if (status.HasValue)
        {
            allOrders = allOrders.Where(o => (OrderStatus)o.StatusId == status.Value);
        }

        var totalOrders = allOrders.Count();
        var totalRevenue = allOrders.Sum(o => o.TotalAmount);
        var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;
        var pendingOrders = allOrders.Count(o => o.StatusId == 0);
        var processingOrders = allOrders.Count(o => o.StatusId == 1);
        var completedOrders = allOrders.Count(o => o.StatusId == 2);

        var totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);
        var orders = allOrders
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var last7DaysStart = DateTime.Now.Date.AddDays(-6);
        var salesData = allOrders
            .Where(o => o.CreatedAt.Date >= last7DaysStart)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new
            {
                date = g.Key,
                total = g.Sum(o => o.TotalAmount)
            })
            .OrderBy(x => x.date)
            .ToList();

        var allDates = Enumerable.Range(0, 7)
            .Select(i => last7DaysStart.AddDays(i))
            .ToList();

        var completeData = allDates.Select(date => new
        {
            total = salesData.FirstOrDefault(s => s.date == date)?.total ?? 0
        });

        var statusStats = new
        {
            new_orders = pendingOrders,
            processing = processingOrders,
            completed = completedOrders
        };

        var totalStats = new
        {
            total_orders = totalOrders,
            total_revenue = totalRevenue,
            average_order = averageOrderValue,
            pending_orders = pendingOrders
        };

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalItems = totalOrders;
        ViewBag.CurrentStatus = status;
        
        ViewBag.SalesData = completeData;
        ViewBag.StatusStats = statusStats;
        ViewBag.TotalStats = totalStats;

        return View(orders);
    }
    
    

    [HttpGet]
    public async Task<IActionResult> GenerateCompleteReport(DateTime? startDate = null, DateTime? endDate = null)
    {
        var allOrders = await _orderRepository.GetAllAsync();
        
        var orders = allOrders.AsEnumerable();
        if (startDate.HasValue)
        {
            orders = orders.Where(o => o.CreatedAt.Date >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            orders = orders.Where(o => o.CreatedAt.Date <= endDate.Value.Date);
        }
        
        var filteredOrders = orders.ToList();

        using var stream = new MemoryStream();
        var document = new Document(PageSize.A4, 25, 25, 30, 30);
        document.Open();
            
        var fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts", "arial.ttf");
        var baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            
        var titleFont = new Font(baseFont, 20, Font.BOLD, new BaseColor(52, 58, 64));
        var title = new Paragraph("ПОВНИЙ ЗВІТ ПО ПРОДАЖАМ ТА ДОСТАВЦІ", titleFont)
        {
            Alignment = Element.ALIGN_CENTER,
            SpacingAfter = 10
        };
        document.Add(title);

        var subtitleFont = new Font(baseFont, 12, Font.NORMAL, new BaseColor(108, 117, 125));
            
        string periodText;
        if (startDate.HasValue && endDate.HasValue)
        {
            periodText = $"Період: {startDate.Value:dd.MM.yyyy} - {endDate.Value:dd.MM.yyyy}";
        }
        else if (startDate.HasValue)
        {
            periodText = $"Період: з {startDate.Value:dd.MM.yyyy}";
        }
        else if (endDate.HasValue)
        {
            periodText = $"Період: до {endDate.Value:dd.MM.yyyy}";
        }
        else
        {
            periodText = "Період: За весь час";
        }
            
        var periodParagraph = new Paragraph(periodText, subtitleFont)
        {
            Alignment = Element.ALIGN_CENTER,
            SpacingAfter = 5
        };
        document.Add(periodParagraph);
            
        var dateText = new Paragraph($"Згенеровано: {DateTime.Now:dd MMMM yyyy р. о HH:mm}", subtitleFont)
        {
            Alignment = Element.ALIGN_CENTER,
            SpacingAfter = 25
        };
        document.Add(dateText);

        var headerFont = new Font(baseFont, 16, Font.BOLD, new BaseColor(52, 58, 64));
        var statsHeader = new Paragraph("ЗАГАЛЬНА СТАТИСТИКА", headerFont)
        {
            SpacingAfter = 15
        };
        document.Add(statsHeader);

        var totalOrders = filteredOrders.Count;
        var totalRevenue = filteredOrders.Sum(o => o.TotalAmount);
        var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;
        var ordersWithDelivery = filteredOrders.Where(o => o.Delivery != null).ToList();
            
        var deliveredCount = ordersWithDelivery.Count(o => o.Delivery.Status == DeliveryStatus.Delivered);
        var pendingCount = ordersWithDelivery.Count(o => o.Delivery.Status == DeliveryStatus.Pending);

        var summaryTable = new PdfPTable(4)
        {
            WidthPercentage = 100
        };
        summaryTable.SetWidths(new float[] { 1, 1, 1, 1 });
        summaryTable.SpacingAfter = 20;

        var cardHeaderFont = new Font(baseFont, 10, Font.BOLD, BaseColor.WHITE);
        var cardBigFont = new Font(baseFont, 20, Font.BOLD, BaseColor.WHITE);
        var cardMediumFont = new Font(baseFont, 16, Font.BOLD, BaseColor.WHITE);

        AddSummaryCard(summaryTable, "ВСЬОГО ЗАМОВЛЕНЬ", totalOrders.ToString(), new BaseColor(40, 167, 69), cardHeaderFont, cardBigFont);
        AddSummaryCard(summaryTable, "ЗАГАЛЬНИЙ ДОХІД", $"₴{totalRevenue:N2}", new BaseColor(0, 123, 255), cardHeaderFont, cardMediumFont);
        AddSummaryCard(summaryTable, "СЕРЕДНІЙ ЧЕК", $"₴{averageOrderValue:N2}", new BaseColor(23, 162, 184), cardHeaderFont, cardMediumFont);
        AddSummaryCard(summaryTable, "ДОСТАВЛЕНО", deliveredCount.ToString(), new BaseColor(255, 193, 7), cardHeaderFont, cardBigFont);

        document.Add(summaryTable);

        AddPeriodStatistics(document, filteredOrders, headerFont, baseFont);
        AddProductStatistics(document, filteredOrders, headerFont, baseFont);
        AddOrderStatistics(document, filteredOrders, headerFont, baseFont);
        AddPaymentStatistics(document, filteredOrders, headerFont, baseFont);
        AddDeliveryStatistics(document, ordersWithDelivery, headerFont, baseFont);
        AddCustomerStatistics(document, filteredOrders, headerFont, baseFont);
        AddTopProducts(document, filteredOrders, headerFont, baseFont);
            
        AddUnpurchasedProducts(document, filteredOrders, headerFont, baseFont);
            
        AddRecentOrders(document, filteredOrders, headerFont, baseFont);

        document.Add(new Paragraph(" "));
        var footerFont = new Font(baseFont, 8, Font.NORMAL, new BaseColor(108, 117, 125));
        var footer = new Paragraph($"Звіт створено автоматично системою управління замовленнями • {DateTime.Now:dd.MM.yyyy HH:mm}", footerFont)
            {
                Alignment = Element.ALIGN_CENTER
            };
        document.Add(footer);

        document.Close();

        var fileName = startDate.HasValue || endDate.HasValue 
            ? $"CompleteReport_{startDate?.ToString("yyyyMMdd") ?? "start"}_{endDate?.ToString("yyyyMMdd") ?? "end"}_{DateTime.Now:HHmmss}.pdf"
            : $"CompleteReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                
        return File(stream.ToArray(), "application/pdf", fileName);
    }

    private void AddUnpurchasedProducts(Document document, IEnumerable<Order> orders, Font headerFont, BaseFont baseFont)
    {
        var unpurchasedHeader = new Paragraph("НЕПРОДАНІ ПРОДУКТИ", headerFont)
        {
            SpacingAfter = 15
        };
        document.Add(unpurchasedHeader);

        var allProducts =  _productRepository.GetAll();
        
        var soldProductIds = orders
            .Where(o => o.OrderItems != null && o.OrderItems.Any())
            .SelectMany(o => o.OrderItems)
            .Select(oi => oi.ProductId)
            .Distinct()
            .ToHashSet();

        var unpurchasedProducts = allProducts
            .Where(p => !soldProductIds.Contains(p.Id))
            .ToList();

        if (unpurchasedProducts.Count == 0)
        {
            var noUnpurchasedFont = new Font(baseFont, 12, Font.NORMAL, new BaseColor(108, 117, 125));
            var noUnpurchasedText = new Paragraph("Всі продукти були продані в обраному періоді!", noUnpurchasedFont);
            noUnpurchasedText.Alignment = Element.ALIGN_CENTER;
            noUnpurchasedText.SpacingAfter = 20;
            document.Add(noUnpurchasedText);
            return;
        }

        var unpurchasedTable = new PdfPTable(3)
        {
            WidthPercentage = 100
        };
        unpurchasedTable.SetWidths([1, 3, 1.5f]);
        unpurchasedTable.SpacingAfter = 20;

        var tableHeaderFont = new Font(baseFont, 12, Font.BOLD, BaseColor.WHITE);
        var tableCellFont = new Font(baseFont, 10, Font.NORMAL, new BaseColor(52, 58, 64));

        AddTableHeader(unpurchasedTable, new[] { "ID", "Назва товару", "Ціна" }, tableHeaderFont, new BaseColor(220, 53, 69));

        var isEvenRow = false;
        foreach (var product in unpurchasedProducts.Take(50))
        {
            var backgroundColor = isEvenRow ? new BaseColor(248, 249, 250) : BaseColor.WHITE;
            
            AddDetailCell(unpurchasedTable, product.Id.ToString(), tableCellFont, backgroundColor);
            AddDetailCell(unpurchasedTable, product.Name ?? "Без назви", tableCellFont, backgroundColor);
            AddDetailCell(unpurchasedTable, $"₴{product.Price:F2}", tableCellFont, backgroundColor);
            
            isEvenRow = !isEvenRow;
        }

        document.Add(unpurchasedTable);

        if (unpurchasedProducts.Count > 50)
        {
            var moreProductsFont = new Font(baseFont, 10, Font.ITALIC, new BaseColor(108, 117, 125));
            var moreProductsText = new Paragraph($"... та ще {unpurchasedProducts.Count - 50} непроданих товарів", moreProductsFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 10
                };
            document.Add(moreProductsText);
        }

        var statsFont = new Font(baseFont, 12, Font.NORMAL, new BaseColor(52, 58, 64));
        var statsText = new Paragraph($"Всього непроданих товарів: {unpurchasedProducts.Count}", statsFont)
            {
                Alignment = Element.ALIGN_CENTER,
                SpacingAfter = 20
            };
        document.Add(statsText);
    }

    [HttpGet]
    public IActionResult ReportPeriodSelection()
    {
        return View();
    }

    private static void AddSummaryCard(PdfPTable table, string title, string value, BaseColor color, Font headerFont, Font valueFont)
    {
        var card = new PdfPCell
        {
            BackgroundColor = color,
            Border = Rectangle.NO_BORDER,
            Padding = 10
        };
        card.AddElement(new Paragraph(title, headerFont) { Alignment = Element.ALIGN_CENTER });
        card.AddElement(new Paragraph(value, valueFont) { Alignment = Element.ALIGN_CENTER });
        table.AddCell(card);
    }

    private void AddPeriodStatistics(Document document, IEnumerable<Order> orders, Font headerFont, BaseFont baseFont)
    {
        var periodHeader = new Paragraph("СТАТИСТИКА ПО ПЕРІОДАХ", headerFont)
        {
            SpacingAfter = 15
        };
        document.Add(periodHeader);

        var today = DateTime.Now.Date;
        var thisWeek = today.AddDays(-(int)today.DayOfWeek);
        var thisMonth = new DateTime(today.Year, today.Month, 1);
        var thisYear = new DateTime(today.Year, 1, 1);

        var todayOrders = orders.Count(o => o.CreatedAt.Date == today);
        var todayRevenue = orders.Where(o => o.CreatedAt.Date == today).Sum(o => o.TotalAmount);
        
        var weekOrders = orders.Count(o => o.CreatedAt.Date >= thisWeek);
        var weekRevenue = orders.Where(o => o.CreatedAt.Date >= thisWeek).Sum(o => o.TotalAmount);
        
        var monthOrders = orders.Count(o => o.CreatedAt.Date >= thisMonth);
        var monthRevenue = orders.Where(o => o.CreatedAt.Date >= thisMonth).Sum(o => o.TotalAmount);
        
        var yearOrders = orders.Count(o => o.CreatedAt.Date >= thisYear);
        var yearRevenue = orders.Where(o => o.CreatedAt.Date >= thisYear).Sum(o => o.TotalAmount);

        var periodTable = new PdfPTable(3)
        {
            WidthPercentage = 100
        };
        periodTable.SetWidths([2, 1, 1.5f]);
        periodTable.SpacingAfter = 20;

        var tableHeaderFont = new Font(baseFont, 12, Font.BOLD, BaseColor.WHITE);
        var tableCellFont = new Font(baseFont, 11, Font.NORMAL, new BaseColor(52, 58, 64));

        AddTableHeader(periodTable, ["Період", "Замовлення", "Дохід"], tableHeaderFont, new BaseColor(52, 58, 64));
        
        AddPeriodRow(periodTable, "Сьогодні", todayOrders, Convert.ToDecimal(todayRevenue), tableCellFont, new BaseColor(248, 249, 250));
        AddPeriodRow(periodTable, "Цей тиждень", weekOrders, Convert.ToDecimal(weekRevenue), tableCellFont, BaseColor.WHITE);
        AddPeriodRow(periodTable, "Цей місяць", monthOrders, Convert.ToDecimal(monthRevenue), tableCellFont, new BaseColor(248, 249, 250));
        AddPeriodRow(periodTable, "Цей рік", yearOrders, Convert.ToDecimal(yearRevenue), tableCellFont, BaseColor.WHITE);

        document.Add(periodTable);
    }

    private static void AddProductStatistics(Document document, IEnumerable<Order> orders, Font headerFont, BaseFont baseFont)
    {
        var productHeader = new Paragraph("СТАТИСТИКА ПО ПРОДУКТАХ", headerFont)
        {
            SpacingAfter = 15
        };
        document.Add(productHeader);

        var allOrderItems = orders
            .Where(o => o.OrderItems.Count != 0)
            .SelectMany(o => o.OrderItems)
            .ToList();

        var totalProductsSold = allOrderItems.Sum(oi => oi.Quantity);
        var uniqueProducts = allOrderItems.Select(oi => oi.ProductId).Distinct().Count();
        var averageProductsPerOrder = orders.Any() ? (double)totalProductsSold / orders.Count() : 0;

        var productStatsTable = new PdfPTable(3)
        {
            WidthPercentage = 100
        };
        productStatsTable.SetWidths(new float[] { 1, 1, 1 });
        productStatsTable.SpacingAfter = 20;

        var cardHeaderFont = new Font(baseFont, 10, Font.BOLD, BaseColor.WHITE);
        var cardBigFont = new Font(baseFont, 20, Font.BOLD, BaseColor.WHITE);

        AddSummaryCard(productStatsTable, "ВСЬОГО ПРОДАНО", totalProductsSold.ToString(), new BaseColor(220, 53, 69), cardHeaderFont, cardBigFont);
        AddSummaryCard(productStatsTable, "УНІКАЛЬНИХ ТОВАРІВ", uniqueProducts.ToString(), new BaseColor(108, 117, 125), cardHeaderFont, cardBigFont);
        AddSummaryCard(productStatsTable, "СЕРЕДНЬО НА ЗАМОВЛЕННЯ", averageProductsPerOrder.ToString("F1"), new BaseColor(23, 162, 184), cardHeaderFont, cardBigFont);

        document.Add(productStatsTable);
    }
    
    private static void AddOrderStatistics(Document document, IEnumerable<Order> orders, Font headerFont, BaseFont baseFont)
    {
        var orderHeader = new Paragraph("СТАТИСТИКА СТАТУСІВ ЗАМОВЛЕНЬ", headerFont)
        {
            SpacingAfter = 15
        };
        document.Add(orderHeader);

        var pendingOrders = orders.Count(o => o.StatusId == 0);
        var processingOrders = orders.Count(o => o.StatusId == 1);
        var completedOrders = orders.Count(o => o.StatusId == 2);
        var cancelledOrders = orders.Count(o => o.StatusId == 3);

        var orderStatusTable = new PdfPTable(3)
        {
            WidthPercentage = 100
        };
        orderStatusTable.SetWidths(new float[] { 2, 1, 1 });
        orderStatusTable.SpacingAfter = 20;

        var tableHeaderFont = new Font(baseFont, 12, Font.BOLD, BaseColor.WHITE);
        var tableCellFont = new Font(baseFont, 11, Font.NORMAL, new BaseColor(52, 58, 64));

        AddTableHeader(orderStatusTable, ["Статус замовлення", "Кількість", "Відсоток"], tableHeaderFont, new BaseColor(40, 167, 69));
        
        var totalOrders = orders.Count();
        AddMethodRow(orderStatusTable, "Нові замовлення", pendingOrders, totalOrders, tableCellFont, new BaseColor(248, 249, 250));
        AddMethodRow(orderStatusTable, "В обробці", processingOrders, totalOrders, tableCellFont, BaseColor.WHITE);
        AddMethodRow(orderStatusTable, "Завершені", completedOrders, totalOrders, tableCellFont, new BaseColor(248, 249, 250));
        AddMethodRow(orderStatusTable, "Скасовані", cancelledOrders, totalOrders, tableCellFont, BaseColor.WHITE);

        document.Add(orderStatusTable);
    }

    private static void AddPaymentStatistics(Document document, IEnumerable<Order> orders, Font headerFont, BaseFont baseFont)
    {
        var paymentHeader = new Paragraph("СТАТИСТИКА ПО ОПЛАТІ", headerFont)
        {
            SpacingAfter = 15
        };
        document.Add(paymentHeader);

        var ordersWithPayment = orders.Where(o => o.Payment != null).ToList();
        var cashPayments = ordersWithPayment.Count(o => o.Payment.PaymentMethod == PaymentMethod.Cash);
        var cardPayments = ordersWithPayment.Count(o => o.Payment.PaymentMethod == PaymentMethod.Card);
        
        var paidOrders = ordersWithPayment.Count(o => o.Payment.Status == PaymentStatus.Success);
        var pendingPayments = ordersWithPayment.Count(o => o.Payment.Status == PaymentStatus.Pending);

        var paymentTable = new PdfPTable(4)
        {
            WidthPercentage = 100
        };
        paymentTable.SetWidths(new float[] { 1, 1, 1, 1 });
        paymentTable.SpacingAfter = 10;

        var cardHeaderFont = new Font(baseFont, 10, Font.BOLD, BaseColor.WHITE);
        var cardBigFont = new Font(baseFont, 20, Font.BOLD, BaseColor.WHITE);

        AddSummaryCard(paymentTable, "ГОТІВКА", cashPayments.ToString(), new BaseColor(40, 167, 69), cardHeaderFont, cardBigFont);
        AddSummaryCard(paymentTable, "КАРТКА", cardPayments.ToString(), new BaseColor(0, 123, 255), cardHeaderFont, cardBigFont);
        AddSummaryCard(paymentTable, "СПЛАЧЕНО", paidOrders.ToString(), new BaseColor(23, 162, 184), cardHeaderFont, cardBigFont);
        AddSummaryCard(paymentTable, "ОЧІКУЄ ОПЛАТИ", pendingPayments.ToString(), new BaseColor(255, 193, 7), cardHeaderFont, cardBigFont);

        document.Add(paymentTable);
        document.Add(new Paragraph(" ") { SpacingAfter = 10 });
    }

    private static void AddDeliveryStatistics(Document document, List<Order> ordersWithDelivery, Font headerFont, BaseFont baseFont)
    {
        var deliveryHeader = new Paragraph("СТАТИСТИКА ПО ДОСТАВЦІ", headerFont)
        {
            SpacingAfter = 15
        };
        document.Add(deliveryHeader);

        var selfPickupCount = ordersWithDelivery.Count(o => o.Delivery.Method == DeliveryMethod.SelfPickup);
        var novaPoshtaCount = ordersWithDelivery.Count(o => o.Delivery.Method == DeliveryMethod.NovaPoshta);
        var courierCount = ordersWithDelivery.Count(o => o.Delivery.Method == DeliveryMethod.Courier);

        var deliveryMethodTable = new PdfPTable(3)
        {
            WidthPercentage = 100
        };
        deliveryMethodTable.SetWidths(new float[] { 2, 1, 1 });
        deliveryMethodTable.SpacingAfter = 20;

        var tableHeaderFont = new Font(baseFont, 12, Font.BOLD, BaseColor.WHITE);
        var tableCellFont = new Font(baseFont, 11, Font.NORMAL, new BaseColor(52, 58, 64));

        AddTableHeader(deliveryMethodTable, ["Метод доставки", "Кількість", "Відсоток"], tableHeaderFont, new BaseColor(52, 58, 64));
        
        var totalDeliveryOrders = ordersWithDelivery.Count;
        AddMethodRow(deliveryMethodTable, "Самовивіз", selfPickupCount, totalDeliveryOrders, tableCellFont, new BaseColor(248, 249, 250));
        AddMethodRow(deliveryMethodTable, "Нова Пошта", novaPoshtaCount, totalDeliveryOrders, tableCellFont, BaseColor.WHITE);
        AddMethodRow(deliveryMethodTable, "Кур'єрська доставка", courierCount, totalDeliveryOrders, tableCellFont, new BaseColor(248, 249, 250));

        document.Add(deliveryMethodTable);
    }

    private static void AddCustomerStatistics(Document document, IEnumerable<Order> orders, Font headerFont, BaseFont baseFont)
    {
        var customerHeader = new Paragraph("СТАТИСТИКА ПО КЛІЄНТАХ", headerFont)
        {
            SpacingAfter = 15
        };
        document.Add(customerHeader);

        var customerOrders = orders.Where(o => o.User != null).GroupBy(o => o.User.Id).ToList();
        var totalCustomers = customerOrders.Count;
        var repeatCustomers = customerOrders.Count(g => g.Count() > 1);
        var averageOrdersPerCustomer = totalCustomers > 0 ? (double)orders.Count() / totalCustomers : 0;
        var maxOrdersPerCustomer = customerOrders.Count != 0 ? customerOrders.Max(g => g.Count()) : 0;

        var customerStatsTable = new PdfPTable(4)
        {
            WidthPercentage = 100
        };
        customerStatsTable.SetWidths(new float[] { 1, 1, 1, 1 });
        customerStatsTable.SpacingAfter = 20;

        var cardHeaderFont = new Font(baseFont, 10, Font.BOLD, BaseColor.WHITE);
        var cardBigFont = new Font(baseFont, 20, Font.BOLD, BaseColor.WHITE);
        var cardMediumFont = new Font(baseFont, 16, Font.BOLD, BaseColor.WHITE);

        AddSummaryCard(customerStatsTable, "ВСЬОГО КЛІЄНТІВ", totalCustomers.ToString(), new BaseColor(111, 66, 193), cardHeaderFont, cardBigFont);
        AddSummaryCard(customerStatsTable, "ПОВТОРНИХ КЛІЄНТІВ", repeatCustomers.ToString(), new BaseColor(220, 53, 69), cardHeaderFont, cardBigFont);
        AddSummaryCard(customerStatsTable, "СЕРЕДНЬО ЗАМОВЛЕНЬ", averageOrdersPerCustomer.ToString("F1"), new BaseColor(253, 126, 20), cardHeaderFont, cardMediumFont);
        AddSummaryCard(customerStatsTable, "МАКСИМУМ ВІД КЛІЄНТА", maxOrdersPerCustomer.ToString(), new BaseColor(32, 201, 151), cardHeaderFont, cardBigFont);

        document.Add(customerStatsTable);
    }

    private static void AddTopProducts(Document document, IEnumerable<Order> orders, Font headerFont, BaseFont baseFont)
    {
        var topProductsHeader = new Paragraph("ТОП-10 ПРОДУКТІВ ЗА КІЛЬКІСТЮ", headerFont)
        {
            SpacingAfter = 15
        };
        document.Add(topProductsHeader);

        var productStats = orders
            .Where(o => o.OrderItems != null && o.OrderItems.Any())
            .SelectMany(o => o.OrderItems)
            .GroupBy(oi => new { oi.ProductId, oi.Product?.Name })
            .Select(g => new
            {
                ProductName = g.Key.Name ?? $"Продукт #{g.Key.ProductId}",
                Quantity = g.Sum(oi => oi.Quantity),
                Revenue = g.Sum(oi => oi.Price * oi.Quantity),
                Orders = g.Select(oi => oi.OrderId).Distinct().Count()
            })
            .OrderByDescending(p => p.Quantity)
            .Take(10)
            .ToList();

        var topProductsTable = new PdfPTable(4)
        {
            WidthPercentage = 100
        };
        topProductsTable.SetWidths([3, 1, 1.5f, 1]);
        topProductsTable.SpacingAfter = 20;

        var tableHeaderFont = new Font(baseFont, 12, Font.BOLD, BaseColor.WHITE);
        var tableCellFont = new Font(baseFont, 10, Font.NORMAL, new BaseColor(52, 58, 64));

        AddTableHeader(topProductsTable, ["Назва товару", "К-ть", "Дохід", "Замовл."], tableHeaderFont, new BaseColor(108, 117, 125));

        bool isEvenRow = false;
        foreach (var product in productStats)
        {
            var backgroundColor = isEvenRow ? new BaseColor(248, 249, 250) : BaseColor.WHITE;
            
            AddDetailCell(topProductsTable, product.ProductName, tableCellFont, backgroundColor);
            AddDetailCell(topProductsTable, product.Quantity.ToString(), tableCellFont, backgroundColor);
            AddDetailCell(topProductsTable, $"₴{product.Revenue:F2}", tableCellFont, backgroundColor);
            AddDetailCell(topProductsTable, product.Orders.ToString(), tableCellFont, backgroundColor);
            
            isEvenRow = !isEvenRow;
        }

        document.Add(topProductsTable);
    }
    
    private static void AddRecentOrders(Document document, IEnumerable<Order> orders, Font headerFont, BaseFont baseFont)
    {
        var recentHeader = new Paragraph("ОСТАННІ 15 ЗАМОВЛЕНЬ", headerFont)
        {
            SpacingAfter = 15
        };
        document.Add(recentHeader);

        var ordersTable = new PdfPTable(7)
        {
            WidthPercentage = 100
        };
        ordersTable.SetWidths([0.8f, 1.2f, 1.5f, 1.2f, 1.2f, 1.2f, 1.2f]);

        var tableHeaderFont = new Font(baseFont, 10, Font.BOLD, BaseColor.WHITE);
        AddTableHeader(ordersTable, ["№", "Дата", "Клієнт", "Статус", "Оплата", "Доставка", "Сума"], tableHeaderFont, new BaseColor(108, 117, 125));

        var detailCellFont = new Font(baseFont, 9, Font.NORMAL, new BaseColor(52, 58, 64));
        var recentOrders = orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(15)
            .ToList();

        var isEvenRow = false;
        foreach (var order in recentOrders)
        {
            var backgroundColor = isEvenRow ? new BaseColor(248, 249, 250) : BaseColor.WHITE;
            
            AddDetailCell(ordersTable, order.Id.ToString(), detailCellFont, backgroundColor);
            AddDetailCell(ordersTable, order.CreatedAt.ToString("dd.MM.yyyy"), detailCellFont, backgroundColor);
            AddDetailCell(ordersTable, order.User.UserName ?? "Невідомо", detailCellFont, backgroundColor);
            AddDetailCell(ordersTable, GetShortOrderStatusName((OrderStatus)order.StatusId), detailCellFont, backgroundColor);
            AddDetailCell(ordersTable, order.Payment != null ? GetShortPaymentStatusName(order.Payment.Status) : "Н/Д", detailCellFont, backgroundColor);
            AddDetailCell(ordersTable, order.Delivery != null ? GetShortDeliveryStatusName(order.Delivery.Status) : "Н/Д", detailCellFont, backgroundColor);
            AddDetailCell(ordersTable, $"₴{order.TotalAmount:F2}", detailCellFont, backgroundColor);
            
            isEvenRow = !isEvenRow;
        }

        document.Add(ordersTable);
    }

    private static void AddTableHeader(PdfPTable table, string[] headers, Font font, BaseColor backgroundColor)
    {
        foreach (var header in headers)
        {
            var cell = new PdfPCell(new Phrase(header, font))
            {
                BackgroundColor = backgroundColor,
                Padding = 10,
                HorizontalAlignment = Element.ALIGN_CENTER
            };
            table.AddCell(cell);
        }
    }

    private static void AddPeriodRow(PdfPTable table, string period, int orders, decimal revenue, Font font, BaseColor backgroundColor)
    {
        AddDetailCell(table, period, font, backgroundColor);
        AddDetailCell(table, orders.ToString(), font, backgroundColor);
        AddDetailCell(table, $"₴{revenue:N2}", font, backgroundColor);
    }

    private static string GetShortOrderStatusName(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.New => "Новий",
            OrderStatus.Processing => "Обробка",
            OrderStatus.Completed => "Завершений",
            _ => "Невідомо"
        };
    }

    private static void AddMethodRow(PdfPTable table, string method, int count, int total, Font font, BaseColor backgroundColor)
    {
        var percentage = total > 0 ? (count * 100.0 / total) : 0;
        
        var methodCell = new PdfPCell(new Phrase(method, font))
        {
            BackgroundColor = backgroundColor,
            Padding = 8
        };
        table.AddCell(methodCell);

        var countCell = new PdfPCell(new Phrase(count.ToString(), font))
        {
            BackgroundColor = backgroundColor,
            Padding = 8,
            HorizontalAlignment = Element.ALIGN_CENTER
        };
        table.AddCell(countCell);

        var percentCell = new PdfPCell(new Phrase($"{percentage:F1}%", font))
        {
            BackgroundColor = backgroundColor,
            Padding = 8,
            HorizontalAlignment = Element.ALIGN_CENTER
        };
        table.AddCell(percentCell);
    }

    private static void AddDetailCell(PdfPTable table, string text, Font font, BaseColor backgroundColor)
    {
        var cell = new PdfPCell(new Phrase(text, font))
        {
            BackgroundColor = backgroundColor,
            Padding = 6
        };
        table.AddCell(cell);
    }
    
    private static string GetShortDeliveryStatusName(DeliveryStatus status)
    {
        return status switch
        {
            DeliveryStatus.Pending => "Очікує",
            DeliveryStatus.Processing => "Обробка",
            DeliveryStatus.Shipped => "Відправлено",
            DeliveryStatus.Delivered => "Доставлено",
            DeliveryStatus.Cancelled => "Скасовано",
            _ => "Невідомо"
        };
    }

    private static string GetShortPaymentStatusName(PaymentStatus status)
    {
        return status switch
        {
            PaymentStatus.Pending => "Очікує",
            PaymentStatus.Processing => "Обробка",
            PaymentStatus.Success => "Сплачено",
            PaymentStatus.Failed => "Помилка",
            PaymentStatus.Cancelled => "Скасовано",
            _ => "Невідомо"
        };
    }

    private static string GetDeliveryStatusName(DeliveryStatus status)
    {
        return status switch
        {
            DeliveryStatus.Pending => "Очікує відправки",
            DeliveryStatus.Processing => "Обробляється",
            DeliveryStatus.Shipped => "Відправлено",
            DeliveryStatus.Delivered => "Доставлено",
            DeliveryStatus.Cancelled => "Скасовано",
            _ => "Невідомий статус"
        };
    }
    
    private static string GetDisplayName(OrderStatus status)
    {
        var field = status.GetType().GetField(status.ToString());
        var attribute = field?.GetCustomAttribute<DisplayAttribute>();
        return attribute?.Name ?? status.ToString();
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
        {
            TempData["ErrorMessage"] = "Замовлення не знайдено.";
            return NotFound();
        }

        var oldStatus = (OrderStatus)order.StatusId;
        order.StatusId = (int)status;
        await _orderRepository.UpdateAsync(order);

        try
        {
            await _emailService.SendOrderStatusUpdateEmailAsync(order);
            TempData["SuccessMessage"] = $"Статус замовлення #{order.Id} успішно змінено з '{GetDisplayName(oldStatus)}' на '{GetDisplayName(status)}'.";
        }
        catch (Exception ex)
        {
            TempData["WarningMessage"] = $"Статус змінено, але не вдалося відправити email: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
        {
            TempData["ErrorMessage"] = "Замовлення не знайдено.";
            return NotFound();
        }

        try
        {
            await _orderRepository.DeleteAsync(id);
            TempData["SuccessMessage"] = $"Замовлення #{id} успішно видалено.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Помилка при видаленні замовлення: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
        {
            TempData["ErrorMessage"] = "Замовлення не знайдено.";
            return NotFound();
        }

        order.Status = (OrderStatus)order.StatusId;

        foreach (var item in order.OrderItems)
        {
            if (item.Product == null)
            {
                item.Product = await _orderRepository.GetProductAsync(item.ProductId);
            }
        }

        return View(order);
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateDeliveryStatus(int id, DeliveryStatus status)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order.Delivery == null)
        {
            TempData["ErrorMessage"] = "Замовлення або доставка не знайдена.";
            return NotFound();
        }

        var oldStatus = order.Delivery.Status;
        order.Delivery.Status = status;
    
        if (status == DeliveryStatus.Delivered)
        {
            order.Delivery.DeliveredAt = DateTime.Now;
        }

        await _orderRepository.UpdateAsync(order);

        try
        {
            await _emailService.SendDeliveryStatusUpdateEmailAsync(order);
            TempData["SuccessMessage"] = $"Статус доставки замовлення #{order.Id} успішно змінено з '{GetDeliveryStatusName(oldStatus)}' на '{GetDeliveryStatusName(status)}'.";
        }
        catch (Exception ex)
        {
            TempData["WarningMessage"] = $"Статус змінено, але не вдалося відправити email: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}