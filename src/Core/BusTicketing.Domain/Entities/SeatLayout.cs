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

    private readonly List<Seat> _seats = new();
    public IReadOnlyCollection<Seat> Seats => _seats.AsReadOnly();

    private SeatLayout() { } // EF Core

    /// <summary>
    /// Generates a layout of Rows x Columns seats labelled A1..A{cols}, B1.. etc.,
    /// matching the row-letter/column-number convention used throughout the system.
    /// </summary>
    public static SeatLayout Generate(Guid busId, int rows, int columns, SeatClass defaultClass = SeatClass.Economy)
    {
        if (rows <= 0 || rows > 26)
            throw new DomainException("Rows must be between 1 and 26.");
        if (columns <= 0)
            throw new DomainException("Columns must be greater than zero.");

        var layout = new SeatLayout
        {
            BusId = busId,
            Rows = rows,
            Columns = columns
        };

        for (var r = 0; r < rows; r++)
        {
            var rowLabel = (char)('A' + r);
            for (var c = 1; c <= columns; c++)
            {
                layout._seats.Add(Seat.Create(layout.Id, $"{rowLabel}{c}", rowLabel.ToString(), c, defaultClass));
            }
        }

        return layout;
    }
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

    private Seat() { } // EF Core

    public static Seat Create(Guid seatLayoutId, string seatNumber, string rowLabel, int columnNumber, SeatClass seatClass)
    {
        return new Seat
        {
            SeatLayoutId = seatLayoutId,
            SeatNumber = seatNumber,
            RowLabel = rowLabel,
            ColumnNumber = columnNumber,
            Class = seatClass,
            IsActive = true
        };
    }

    public void SetOutOfService() => IsActive = false;
    public void SetInService() => IsActive = true;
    public void Reclassify(SeatClass seatClass) => Class = seatClass;
}
