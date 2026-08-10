namespace BusTicketing.Application.Features.Reports;

public class RevenueReportDto
{
    public DateOnly Date { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalTicketsSold { get; set; }
    public int TotalSeatsAvailable { get; set; }
    public decimal OccupancyRate { get; set; }

    public RevenueReportDto() { }

    public RevenueReportDto(DateOnly date, decimal totalRevenue, int totalTicketsSold, int totalSeatsAvailable, decimal occupancyRate)
    {
        Date = date;
        TotalRevenue = totalRevenue;
        TotalTicketsSold = totalTicketsSold;
        TotalSeatsAvailable = totalSeatsAvailable;
        OccupancyRate = occupancyRate;
    }
}

public record RevenueReportRequest(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    Guid? RouteId = null);

public record OccupancyReportDto(
    string BusNumber,
    string RouteName,
    DateOnly TravelDate,
    TimeOnly DepartureTime,
    int TotalSeats,
    int SoldSeats,
    decimal OccupancyRate,
    decimal Revenue);

public record TopRouteDto(
    string RouteName,
    int TotalTicketsSold,
    decimal TotalRevenue,
    decimal AverageFare);
