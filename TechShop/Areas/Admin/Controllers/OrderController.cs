using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechShop.Interfaces;
using TechShop.Models;

[Authorize(Roles = "Admin")]
public class OrderController : Controller
{
    private readonly IOrderRepository _orderRepository;

    public OrderController(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
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
}