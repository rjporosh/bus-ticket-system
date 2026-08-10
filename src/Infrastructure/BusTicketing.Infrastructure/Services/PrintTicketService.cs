using BusTicketing.Application.Common.Interfaces;
using BusTicketing.Application.Features.Booking;
using BusTicketing.Domain.Enums;

namespace BusTicketing.Infrastructure.Services;

public class PrintTicketService : IPrintTicketService
{
    public Task<string> GenerateHtmlAsync(PrintTicketDto ticket, CancellationToken cancellationToken = default)
    {
        var statusLabel = ticket.Status == TicketStatus.Sold ? "Sold" : "Cancelled";
        var statusColor = ticket.Status == TicketStatus.Sold ? "#2e7d32" : "#c62828";
        var statusBg = ticket.Status == TicketStatus.Sold ? "#e8f5e9" : "#ffebee";
        var ageDisplay = ticket.Age.HasValue ? $"{ticket.Age} years" : "—";
        var nidDisplay = string.IsNullOrWhiteSpace(ticket.NidOrPassport) ? "—" : ticket.NidOrPassport;
        var cancellationDisplay = string.IsNullOrWhiteSpace(ticket.CancellationReason) ? "—" : ticket.CancellationReason;
        var cancelledAtDisplay = ticket.CancelledAtUtc.HasValue ? ticket.CancelledAtUtc.Value.ToString("yyyy-MM-dd HH:mm") : "—";

        var cancellationBox = ticket.Status == TicketStatus.Cancelled
            ? "<div class=\"cancellation-box\">" +
              "<div class=\"info-label\">Cancellation Reason</div>" +
              "<div class=\"info-value\">" + cancellationDisplay + "</div>" +
              "<div class=\"info-label\" style=\"margin-top: 4px;\">Cancelled At</div>" +
              "<div class=\"info-value\">" + cancelledAtDisplay + "</div>" +
              "</div>"
            : "";

        var html = "<!DOCTYPE html>" +
            "<html lang=\"en\">" +
            "<head>" +
            "<meta charset=\"UTF-8\">" +
            "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" +
            "<title>Ticket " + ticket.TicketNumber + "</title>" +
            "<style>" +
            "* { box-sizing: border-box; margin: 0; padding: 0; }" +
            "body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f5f5f5; padding: 20px; }" +
            ".ticket-container { max-width: 700px; margin: 0 auto; background: #fff; border-radius: 8px; box-shadow: 0 2px 12px rgba(0,0,0,0.1); overflow: hidden; }" +
            ".ticket-header { background: linear-gradient(135deg, #1a73e8, #0d47a1); color: #fff; padding: 24px; text-align: center; }" +
            ".ticket-header h1 { font-size: 1.5rem; margin-bottom: 4px; }" +
            ".ticket-header p { font-size: 0.9rem; opacity: 0.9; }" +
            ".ticket-body { padding: 24px; }" +
            ".ticket-number { font-size: 1.25rem; font-weight: 700; color: #1a73e8; text-align: center; margin-bottom: 16px; font-family: 'Courier New', monospace; }" +
            ".info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-bottom: 16px; }" +
            ".info-item { background: #f8f9fa; padding: 10px 12px; border-radius: 6px; border-left: 3px solid #1a73e8; }" +
            ".info-item.full { grid-column: span 2; }" +
            ".info-label { font-size: 0.75rem; text-transform: uppercase; color: #666; letter-spacing: 0.5px; margin-bottom: 2px; }" +
            ".info-value { font-size: 0.95rem; font-weight: 600; color: #333; }" +
            ".fare-section { text-align: center; padding: 16px; background: #e8f5e9; border-radius: 6px; margin: 16px 0; }" +
            ".fare-label { font-size: 0.85rem; color: #2e7d32; }" +
            ".fare-amount { font-size: 1.75rem; font-weight: 700; color: #1b5e20; }" +
            ".status-badge { display: inline-block; padding: 4px 12px; border-radius: 4px; font-size: 0.85rem; font-weight: 600; color: " + statusColor + "; background: " + statusBg + "; }" +
            ".cancellation-box { background: #ffebee; border-left: 3px solid #c62828; padding: 12px; border-radius: 6px; margin-top: 12px; }" +
            ".cancellation-box .info-label { color: #c62828; }" +
            ".footer { text-align: center; padding: 16px; color: #888; font-size: 0.8rem; border-top: 1px solid #eee; }" +
            "@media print {" +
            "body { background: #fff; padding: 0; }" +
            ".ticket-container { box-shadow: none; max-width: 100%; }" +
            "@page { margin: 10mm; }" +
            "}" +
            "</style>" +
            "</head>" +
            "<body>" +
            "<div class=\"ticket-container\">" +
            "<div class=\"ticket-header\">" +
            "<h1>Bus Ticketing System</h1>" +
            "<p>Official Travel Ticket</p>" +
            "</div>" +
            "<div class=\"ticket-body\">" +
            "<div class=\"ticket-number\">" + ticket.TicketNumber + "</div>" +
            "<div style=\"text-align: center; margin-bottom: 16px;\">" +
            "<span class=\"status-badge\">" + statusLabel + "</span>" +
            "</div>" +
            "<div class=\"info-grid\">" +
            "<div class=\"info-item\">" +
            "<div class=\"info-label\">Passenger</div>" +
            "<div class=\"info-value\">" + ticket.PassengerName + "</div>" +
            "</div>" +
            "<div class=\"info-item\">" +
            "<div class=\"info-label\">Mobile</div>" +
            "<div class=\"info-value\">" + ticket.MobileNumber + "</div>" +
            "</div>" +
            "<div class=\"info-item\">" +
            "<div class=\"info-label\">Route</div>" +
            "<div class=\"info-value\">" + ticket.RouteName + "</div>" +
            "</div>" +
            "<div class=\"info-item\">" +
            "<div class=\"info-label\">Bus</div>" +
            "<div class=\"info-value\">" + ticket.BusNumber + "</div>" +
            "</div>" +
            "<div class=\"info-item\">" +
            "<div class=\"info-label\">Seat</div>" +
            "<div class=\"info-value\">" + ticket.SeatNumber + "</div>" +
            "</div>" +
            "<div class=\"info-item\">" +
            "<div class=\"info-label\">Travel Date</div>" +
            "<div class=\"info-value\">" + ticket.TravelDate.ToString("yyyy-MM-dd") + "</div>" +
            "</div>" +
            "<div class=\"info-item\">" +
            "<div class=\"info-label\">Departure</div>" +
            "<div class=\"info-value\">" + ticket.DepartureTime.ToString("hh\\:mm") + "</div>" +
            "</div>" +
            "<div class=\"info-item\">" +
            "<div class=\"info-label\">Age / Gender</div>" +
            "<div class=\"info-value\">" + ageDisplay + " " + (ticket.Gender ?? "") + "</div>" +
            "</div>" +
            "<div class=\"info-item full\">" +
            "<div class=\"info-label\">NID / Passport</div>" +
            "<div class=\"info-value\">" + nidDisplay + "</div>" +
            "</div>" +
            "</div>" +
            "<div class=\"fare-section\">" +
            "<div class=\"fare-label\">Total Fare Paid</div>" +
            "<div class=\"fare-amount\">৳" + ticket.FareAmount.ToString("N2") + "</div>" +
            "</div>" +
            "<div class=\"info-grid\">" +
            "<div class=\"info-item\">" +
            "<div class=\"info-label\">Sold By</div>" +
            "<div class=\"info-value\">" + ticket.SoldByUsername + "</div>" +
            "</div>" +
            "<div class=\"info-item\">" +
            "<div class=\"info-label\">Sold At</div>" +
            "<div class=\"info-value\">" + ticket.SoldAtUtc.ToString("yyyy-MM-dd HH:mm") + "</div>" +
            "</div>" +
            "</div>" +
            cancellationBox +
            "</div>" +
            "<div class=\"footer\">" +
            "Generated on " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + " UTC | Bus Ticketing System" +
            "</div>" +
            "</div>" +
            "<script>window.onload = function() { window.print(); }</script>" +
            "</body>" +
            "</html>";

        return Task.FromResult(html);
    }
}
