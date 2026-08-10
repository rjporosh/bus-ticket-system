namespace BusTicketing.Application.Common.Models;

public class PaymentGatewaySettings
{
    public const string SectionName = "PaymentGateway";

    public string Provider { get; set; } = "Mock";
    public bool EnableRealGateway { get; set; } = false;

    public BkashSettings Bkash { get; set; } = new();
    public NagadSettings Nagad { get; set; } = new();
    public CardGatewaySettings CardGateway { get; set; } = new();
}

public class BkashSettings
{
    public string BaseUrl { get; set; } = "https://tokenized.sandbox.bka.sh/v1.2.0-beta";
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string AppKey { get; set; } = default!;
    public string AppSecret { get; set; } = default!;
    public string MerchantId { get; set; } = default!;
    public int WebhookPort { get; set; } = 5000;
}

public class NagadSettings
{
    public string BaseUrl { get; set; } = "https://sandbox.nagad.com.bd/api/v2";
    public string MerchantId { get; set; } = default!;
    public string PublicKey { get; set; } = default!;
    public string PrivateKey { get; set; } = default!;
    public int WebhookPort { get; set; } = 5000;
}

public class CardGatewaySettings
{
    public string BaseUrl { get; set; } = default!;
    public string ApiKey { get; set; } = default!;
    public string MerchantId { get; set; } = default!;
}
