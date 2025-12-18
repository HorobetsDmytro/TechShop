using TechShop.Models;

namespace TechShop.Services;

public interface ILiqPayService
{
    string GeneratePaymentForm(Order order, string returnUrl, string callbackUrl);
    string GenerateSignature(string data);
    bool VerifyCallback(string data, string signature);
    Dictionary<string, object> ParseCallbackData(string data);
    Task<Dictionary<string, object>> GetStatusAsync(string orderId);
}