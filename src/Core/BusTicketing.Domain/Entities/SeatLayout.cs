using BusTicketing.Domain.Common;
using BusTicketing.Domain.Enums;
using BusTicketing.Domain.Exceptions;

namespace BusTicketing.Domain.Entities;

/// <summary>
/// The physical seat grid for a bus (e.g. 6 rows x 4 columns = 24 seats).
/// This is a template: it is not per-trip. Per-trip availability is derived
/// by the (out-of-phase) Booking module against this layout.
/// </summary>
public class SeatLayout : BaseEntity
{
    public Guid BusId { get; private set; }
    public int Rows { get; private set; }
    public int Columns { get; private set; }
    public LayoutType LayoutType { get; private set; } = LayoutType.StandardGrid;
    public string? LayoutConfigJson { get; private set; }

    private readonly List<Seat> _seats = new();
    public IReadOnlyCollection<Seat> Seats => _seats.AsReadOnly();

    private SeatLayout() { } // EF Core

    /// <summary>
    /// Generates a layout of Rows x Columns seats labelled A1..A{cols}, B1.. etc.,
    /// matching the row-letter/column-number convention used throughout the system.
    /// </summary>
    public static SeatLayout Generate(Guid busId, int rows, int columns, SeatClass defaultClass = SeatClass.Economy, LayoutType layoutType = LayoutType.StandardGrid, string? layoutConfigJson = null)
    {
        if (rows <= 0 || rows > 26)
            throw new DomainException("Rows must be between 1 and 26.");
        if (columns <= 0)
            throw new DomainException("Columns must be greater than zero.");

        var layout = new SeatLayout
        {
            BusId = busId,
            Rows = rows,
            Columns = columns,
            LayoutType = layoutType,
            LayoutConfigJson = layoutConfigJson
        };

        if (layoutType == LayoutType.RealBus && !string.IsNullOrWhiteSpace(layoutConfigJson))
        {
            GenerateRealBusLayout(layout, rows, columns, defaultClass, layoutConfigJson);
        }
        else
        {
            GenerateStandardGrid(layout, rows, columns, defaultClass);
        }

        return layout;
    }

    private static void GenerateStandardGrid(SeatLayout layout, int rows, int columns, SeatClass defaultClass)
    {
        for (var r = 0; r < rows; r++)
        {
            var rowLabel = (char)('A' + r);
            for (var c = 1; c <= columns; c++)
            {
                layout._seats.Add(Seat.Create(layout.Id, $"{rowLabel}{c}", rowLabel.ToString(), c, defaultClass));
            }
        }
    }

    private static void GenerateRealBusLayout(SeatLayout layout, int rows, int columns, SeatClass defaultClass, string configJson)
    {
        // Default real-bus config: driver seat front-left, then split evenly
        // left/right of the aisle, with an aisle gap of 1.
        var config = System.Text.Json.JsonSerializer.Deserialize<RealBusConfig>(configJson) ?? new RealBusConfig();

        // Default left/right split when the caller didn't provide an explicit
        // per-row breakdown: derive it from 'columns' (the actual number of
        // seats per normal row), not a hardcoded constant. E.g. columns=4 ->
        // 2 left + 2 right; columns=5 -> 3 left + 2 right.
        var defaultLeft = (columns + 1) / 2;
        var defaultRight = columns / 2;

        for (var r = 0; r < rows; r++)
        {
            var rowLabel = (char)('A' + r);
            var seatIndex = 1;

            if (r == 0 && config.DriverSeat)
            {
                layout._seats.Add(Seat.Create(layout.Id, $"{rowLabel}{seatIndex}", rowLabel.ToString(), seatIndex, defaultClass, true));
                seatIndex++;
            }

            var leftSeats = r == rows - 1 && config.LastRowConfig != null
                ? config.LastRowConfig.Left
                : (config.SeatsPerRow != null && config.SeatsPerRow.Count > r ? config.SeatsPerRow[r].Left : defaultLeft);
            var rightSeats = r == rows - 1 && config.LastRowConfig != null
                ? config.LastRowConfig.Right
                : (config.SeatsPerRow != null && config.SeatsPerRow.Count > r ? config.SeatsPerRow[r].Right : defaultRight);

            for (var s = 0; s < leftSeats; s++)
            {
                layout._seats.Add(Seat.Create(layout.Id, $"{rowLabel}{seatIndex}", rowLabel.ToString(), seatIndex, defaultClass));
                seatIndex++;
            }

            // The aisle is a walking-gap visual only (see visualCol mapping in
            // SeatLayoutFeature/GetAvailableSeats); it must never consume a
            // seat-number index or cause a number to be skipped, so seatIndex
            // is deliberately NOT advanced by config.AisleGap here.

            for (var s = 0; s < rightSeats; s++)
            {
                layout._seats.Add(Seat.Create(layout.Id, $"{rowLabel}{seatIndex}", rowLabel.ToString(), seatIndex, defaultClass));
                seatIndex++;
            }
        }
    }

    public void SetLayoutConfig(string? configJson)
    {
        LayoutConfigJson = configJson;
    }
}

public class RealBusConfig
{
    public bool DriverSeat { get; set; } = true;
    public int AisleGap { get; set; } = 1;
    public List<RowSeatGroup>? SeatsPerRow { get; set; }
    public RowSeatGroup? LastRowConfig { get; set; }
}

public class RowSeatGroup
{
    public int Left { get; set; } = 2;
    public int Right { get; set; } = 2;
}

/// <summary>A single seat within a bus's layout, e.g. "A1".</summary>
public class Seat : BaseEntity
{
    public Guid SeatLayoutId { get; private set; }
    public string SeatNumber { get; private set; } = default!;
    public string RowLabel { get; private set; } = default!;
    public int ColumnNumber { get; private set; }
    public SeatClass Class { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsDriver { get; private set; } = false;

    private Seat() { } // EF Core

    public static Seat Create(Guid seatLayoutId, string seatNumber, string rowLabel, int columnNumber, SeatClass seatClass, bool isDriver = false)
    {
        return new Seat
        {
            SeatLayoutId = seatLayoutId,
            SeatNumber = seatNumber,
            RowLabel = rowLabel,
            ColumnNumber = columnNumber,
            Class = seatClass,
            IsActive = true,
            IsDriver = isDriver
        };
    }

    public void SetOutOfService() => IsActive = false;
    public void SetInService() => IsActive = true;
    public void Reclassify(SeatClass seatClass) => Class = seatClass;
}
