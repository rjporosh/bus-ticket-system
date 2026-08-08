using BusTicketing.Domain.Common;

namespace BusTicketing.Domain.Entities;

public class TicketNumberCounter : BaseEntity
{
    public DateOnly CounterDate { get; private set; }
    public int LastNumber { get; private set; }

    private TicketNumberCounter() { }

    public static TicketNumberCounter Create(DateOnly counterDate, int lastNumber = 0)
    {
        return new TicketNumberCounter
        {
            CounterDate = counterDate,
            LastNumber = lastNumber,
        };
    }

    public int Next(int batchSize = 1)
    {
        var next = LastNumber + batchSize;
        LastNumber = next;
        return next;
    }
}
