namespace BusTicketing.Application.Features.Dashboard;

public class DashboardSummaryDto
{
    public DateOnly Date { get; set; }
    public int TotalSeats { get; set; }
    public int SoldSeats { get; set; }
    public int AvailableSeats { get; set; }
    public decimal TotalSales { get; set; }
    public List<RouteSalesDto> RouteWiseSales { get; set; } = new();
    public List<BusSeatStatusDto> BusWiseSeatStatus { get; set; } = new();

    public DashboardSummaryDto(
        DateOnly date,
        int totalSeats,
        int soldSeats,
        int availableSeats,
        decimal totalSales,
        List<RouteSalesDto> routeWiseSales,
        List<BusSeatStatusDto> busWiseSeatStatus)
    {
        Date = date;
        TotalSeats = totalSeats;
        SoldSeats = soldSeats;
        AvailableSeats = availableSeats;
        TotalSales = totalSales;
        RouteWiseSales = routeWiseSales;
        BusWiseSeatStatus = busWiseSeatStatus;
    }
}

/// <summary>
/// Per-route breakdown of sales and seats.
/// </summary>
public record RouteSalesDto(
    string RouteName,
    int SoldSeats,
    int AvailableSeats,
    decimal TotalSales);

/// <summary>
/// Per-bus breakdown of seat status.
/// </summary>
public record BusSeatStatusDto(
    string BusNumber,
    string RouteName,
    TimeOnly DepartureTime,
    int AvailableSeats,
    int TotalSeats);