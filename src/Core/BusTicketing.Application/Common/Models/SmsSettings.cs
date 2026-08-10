namespace BusTicketing.Application.Common.Models;

public class SmsSettings
{
    public const string SectionName = "Sms";

    public string Provider { get; set; } = "None";
    public bool EnableNotifications { get; set; } = false;

    public TwilioSettings Twilio { get; set; } = new();
    public GsmGatewaySettings GsmGateway { get; set; } = new();
}

public class TwilioSettings
{
    public string AccountSid { get; set; } = default!;
    public string AuthToken { get; set; } = default!;
    public string FromNumber { get; set; } = default!;
}

public class GsmGatewaySettings
{
    public string BaseUrl { get; set; } = default!;
    public string ApiKey { get; set; } = default!;
    public string SenderId { get; set; } = default!;
}
