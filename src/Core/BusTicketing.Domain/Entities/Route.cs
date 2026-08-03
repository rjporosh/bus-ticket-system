using BusTicketing.Domain.Common;
using BusTicketing.Domain.Exceptions;

namespace BusTicketing.Domain.Entities;

/// <summary>A directional path between two stations, e.g. Dhaka -&gt; Chittagong.</summary>
public class Route : BaseEntity
{
    public string Name { get; private set; } = default!;

    public Guid OriginStationId { get; private set; }
    public Station Origin { get; private set; } = default!;

    public Guid DestinationStationId { get; private set; }
    public Station Destination { get; private set; } = default!;

    public decimal DistanceKm { get; private set; }
    public int EstimatedDurationMinutes { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Route() { } // EF Core

    public static Route Create(
        string name,
        Guid originStationId,
        Guid destinationStationId,
        decimal distanceKm,
        int estimatedDurationMinutes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Route name is required.");
        if (originStationId == destinationStationId)
            throw new DomainException("Origin and destination stations must be different.");
        if (distanceKm <= 0)
            throw new DomainException("Distance must be greater than zero.");
        if (estimatedDurationMinutes <= 0)
            throw new DomainException("Estimated duration must be greater than zero.");

        return new Route
        {
            Name = name.Trim(),
            OriginStationId = originStationId,
            DestinationStationId = destinationStationId,
            DistanceKm = distanceKm,
            EstimatedDurationMinutes = estimatedDurationMinutes,
            IsActive = true
        };
    }

    public void Update(string name, decimal distanceKm, int estimatedDurationMinutes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Route name is required.");
        if (distanceKm <= 0)
            throw new DomainException("Distance must be greater than zero.");
        if (estimatedDurationMinutes <= 0)
            throw new DomainException("Estimated duration must be greater than zero.");

        Name = name.Trim();
        DistanceKm = distanceKm;
        EstimatedDurationMinutes = estimatedDurationMinutes;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
