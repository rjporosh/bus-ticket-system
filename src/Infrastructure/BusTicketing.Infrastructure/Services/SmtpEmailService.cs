using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Common.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace BusTicketing.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public SmtpEmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendBookingConfirmationAsync(string toEmail, string passengerName, string ticketNumber, string routeName, string busNumber, DateOnly travelDate, TimeOnly departureTime, string seatNumber, decimal fareAmount, CancellationToken cancellationToken = default)
    {
        if (!_settings.EnableNotifications)
            return;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = $"Booking Confirmed — Ticket {ticketNumber}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $"""
                <html>
                <body style="font-family: Arial, sans-serif; color: #333;">
                    <h2 style="color: #1a73e8;">Booking Confirmed</h2>
                    <p>Dear {System.Net.WebUtility.HtmlEncode(passengerName)},</p>
                    <p>Your ticket has been booked successfully. Here are your booking details:</p>
                    <table style="border-collapse: collapse; width: 100%; max-width: 480px;">
                        <tr style="background: #f8f9fa;">
                            <td style="padding: 8px 12px; border: 1px solid #ddd; font-weight: 600;">Ticket Number</td>
                            <td style="padding: 8px 12px; border: 1px solid #ddd;">{System.Net.WebUtility.HtmlEncode(ticketNumber)}</td>
                        </tr>
                        <tr>
                            <td style="padding: 8px 12px; border: 1px solid #ddd; font-weight: 600;">Route</td>
                            <td style="padding: 8px 12px; border: 1px solid #ddd;">{System.Net.WebUtility.HtmlEncode(routeName)}</td>
                        </tr>
                        <tr style="background: #f8f9fa;">
                            <td style="padding: 8px 12px; border: 1px solid #ddd; font-weight: 600;">Bus</td>
                            <td style="padding: 8px 12px; border: 1px solid #ddd;">{System.Net.WebUtility.HtmlEncode(busNumber)}</td>
                        </tr>
                        <tr>
                            <td style="padding: 8px 12px; border: 1px solid #ddd; font-weight: 600;">Date</td>
                            <td style="padding: 8px 12px; border: 1px solid #ddd;">{travelDate:yyyy-MM-dd}</td>
                        </tr>
                        <tr style="background: #f8f9fa;">
                            <td style="padding: 8px 12px; border: 1px solid #ddd; font-weight: 600;">Departure</td>
                            <td style="padding: 8px 12px; border: 1px solid #ddd;">{departureTime:hh\\:mm}</td>
                        </tr>
                        <tr>
                            <td style="padding: 8px 12px; border: 1px solid #ddd; font-weight: 600;">Seat</td>
                            <td style="padding: 8px 12px; border: 1px solid #ddd;">{System.Net.WebUtility.HtmlEncode(seatNumber)}</td>
                        </tr>
                        <tr style="background: #f8f9fa;">
                            <td style="padding: 8px 12px; border: 1px solid #ddd; font-weight: 600;">Fare Paid</td>
                            <td style="padding: 8px 12px; border: 1px solid #ddd;">৳{fareAmount:N2}</td>
                        </tr>
                    </table>
                    <p style="margin-top: 16px;">Please present this email or your QR code at boarding.</p>
                    <p style="color: #888; font-size: 0.875rem;">Bus Ticketing System</p>
                </body>
                </html>
                """,
            TextBody = $"Booking Confirmed\n\nTicket Number: {ticketNumber}\nRoute: {routeName}\nBus: {busNumber}\nDate: {travelDate:yyyy-MM-dd}\nDeparture: {departureTime:hh\\:mm}\nSeat: {seatNumber}\nFare Paid: ৳{fareAmount:N2}\n\nPlease present this email at boarding.\nBus Ticketing System"
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            var secureSocketOptions = _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, secureSocketOptions, cancellationToken);
            if (!string.IsNullOrWhiteSpace(_settings.SmtpUsername))
                await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword, cancellationToken);
            await client.SendAsync(message, cancellationToken);
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
