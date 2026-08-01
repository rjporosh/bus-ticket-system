using BusTicketing.Domain.Common;
using BusTicketing.Domain.Exceptions;

namespace BusTicketing.Domain.Entities;

/// <summary>A physical boarding/drop-off point, e.g. "Dhaka - Gabtoli Bus Terminal".</summary>
public class Station : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string City { get; private set; } = default!;
    public string? Address { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Station() { } // EF Core

    public static Station Create(string name, string city, string? address = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Station name is required.");
        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException("Station city is required.");

        return new Station
        {
            Name = name.Trim(),
            City = city.Trim(),
            Address = address,
            IsActive = true
        };
    }

    public void Update(string name, string city, string? address)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Station name is required.");
        if (string.IsNullOrWhiteSpace(city))
            throw new DomainException("Station city is required.");

        Name = name.Trim();
        City = city.Trim();
        Address = address;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
