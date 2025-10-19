using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TechShop.Models;
using Microsoft.Extensions.Options;

namespace TechShop.Services;

public class LiqPayService : ILiqPayService
{
    private readonly LiqPaySettings _settings;
    private readonly ILogger<LiqPayService> _logger;

    public LiqPayService(IOptions<LiqPaySettings> settings, ILogger<LiqPayService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    
        _logger.LogInformation("LiqPay settings loaded: PublicKey={SettingsPublicKey}, Sandbox={SettingsSandboxMode}", _settings.PublicKey, _settings.SandboxMode);
    }

    public string GeneratePaymentForm(Order order, string returnUrl, string callbackUrl)
    {
        try
        {
            _logger.LogInformation("Generating payment form for order {OrderId}", order.Id);
            _logger.LogInformation("Public Key: {SettingsPublicKey}", _settings.PublicKey);
            _logger.LogInformation("Sandbox Mode: {SettingsSandboxMode}", _settings.SandboxMode);

            var paymentData = new Dictionary<string, object>
            {
                ["public_key"] = _settings.PublicKey,
                ["version"] = "3",
                ["action"] = "pay",
                ["amount"] = Math.Round(order.TotalWithDelivery, 2),
                ["currency"] = "UAH",
                ["description"] = $"Оплата замовлення №{order.Id} - Граланд",
                ["order_id"] = order.Id.ToString(),
                ["sandbox"] = _settings.SandboxMode ? 1 : 0,
                ["server_url"] = callbackUrl,
                ["result_url"] = returnUrl,
                ["language"] = "uk"
            };

            var jsonData = JsonSerializer.Serialize(paymentData, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _logger.LogInformation("Payment data JSON: {JsonData}", jsonData);

            var data = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonData));
            var signature = GenerateSignature(data);

            _logger.LogInformation("Generated signature: {Signature}", signature);

            return $@"
                <form method='POST' action='https://www.liqpay.ua/api/3/checkout' accept-charset='utf-8' id='liqpay-form' style='display:none;'>
                    <input type='hidden' name='data' value='{data}' />
                    <input type='hidden' name='signature' value='{signature}' />
                </form>
                <script>
                    console.log('Submitting LiqPay form...');
                    document.getElementById('liqpay-form').submit();
                </script>";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payment form");
            throw;
        }
    }

    public string GenerateSignature(string data)
    {
        try
        {
            var signatureString = _settings.PrivateKey + data + _settings.PrivateKey;
            _logger.LogInformation($"Signature string length: {signatureString.Length}");
            var hash = SHA1.HashData(Encoding.UTF8.GetBytes(signatureString));
            var signature = Convert.ToBase64String(hash);
            return signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating signature");
            throw;
        }
    }

    public bool VerifyCallback(string data, string signature)
    {
        try
        {
            var expectedSignature = GenerateSignature(data);
            var isValid = expectedSignature == signature;
            
            _logger.LogInformation("Callback verification: {IsValid}", isValid);
            if (!isValid)
            {
                _logger.LogWarning("Expected: {ExpectedSignature}, Received: {Signature}", expectedSignature, signature);
            }
            
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying callback");
            return false;
        }
    }

    public Dictionary<string, object> ParseCallbackData(string data)
    {
        try
        {
            var jsonString = Encoding.UTF8.GetString(Convert.FromBase64String(data));
            _logger.LogInformation("Callback data: {JsonString}", jsonString);
            
            return JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString) ?? 
                   new Dictionary<string, object>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing callback data");
            return new Dictionary<string, object>();
        }
    }
}