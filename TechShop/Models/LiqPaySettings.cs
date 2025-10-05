namespace TechShop.Models;

public class LiqPaySettings
{
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public bool SandboxMode { get; set; } = true;
    public string ServerUrl { get; set; } = string.Empty;
    public string ResultUrl { get; set; } = string.Empty;
}