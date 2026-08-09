using BusTicketing.Domain.Common;
using BusTicketing.Domain.Exceptions;

namespace BusTicketing.Domain.Entities;

/// <summary>A physical vehicle in the fleet, e.g. "Bus-1". Owns exactly one seat layout.</summary>
public class Bus : BaseEntity
{
    public string Number { get; private set; } = default!;
    public string RegistrationNumber { get; private set; } = default!;
    public string OperatorName { get; private set; } = default!;
    public int TotalSeats { get; private set; }
    public bool IsActive { get; private set; } = true;

    public SeatLayout? SeatLayout { get; private set; }

    private Bus() { } // EF Core

    public static Bus Create(string number, string registrationNumber, string operatorName, int totalSeats)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new DomainException("Bus number is required.");
        if (string.IsNullOrWhiteSpace(registrationNumber))
            throw new DomainException("Registration number is required.");
        if (totalSeats <= 0)
            throw new DomainException("Total seats must be greater than zero.");

        return new Bus
        {
            Number = number.Trim(),
            RegistrationNumber = registrationNumber.Trim().ToUpperInvariant(),
            OperatorName = operatorName.Trim(),
            TotalSeats = totalSeats,
            IsActive = true
        };
    }

    public void Update(string number, string operatorName)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new DomainException("Bus number is required.");

        Number = number.Trim();
        OperatorName = operatorName.Trim();
    }

    /// <summary>
    /// Attaches a generated seat layout to this bus. TotalSeats must match the layout's
    /// seat count exactly so downstream schedule/seat-map logic never has to reconcile
    /// two different sources of truth.
    /// </summary>
    public void AssignSeatLayout(SeatLayout layout)
    {
        if (layout.Seats.Count != TotalSeats)
            throw new DomainException(
                $"Seat layout has {layout.Seats.Count} seats but bus is configured for {TotalSeats}.");

        SeatLayout = layout;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
    public void SetTotalSeats(int totalSeats) => TotalSeats = totalSeats;
}
