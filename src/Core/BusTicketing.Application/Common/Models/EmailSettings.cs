namespace BusTicketing.Application.Common.Models;

public class EmailSettings
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = default!;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = default!;
    public string SmtpPassword { get; set; } = default!;
    public string FromEmail { get; set; } = default!;
    public string FromName { get; set; } = "Bus Ticketing System";
    public bool EnableSsl { get; set; } = true;
    public bool EnableNotifications { get; set; } = false;
}
