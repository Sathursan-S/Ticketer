namespace SharedLibrary.Contracts.Messages;

public record HoldTickets
{
    public Guid BookingId { get; init; }
    public long EventId { get; init; }
    public int NumberOfTickets { get; init; }
    public string CustomerId { get; init; } = string.Empty;
}

public record ProcessPayment
{
    public Guid BookingId { get; init; }
    public string CustomerId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = "card";
}

public record ReserveTickets
{
    public Guid BookingId { get; init; }
    public long EventId { get; init; }
    public List<Guid> TicketIds { get; init; } = new();
    public string CustomerId { get; init; } = string.Empty;
}

public record ReleaseTickets
{
    public Guid BookingId { get; init; }
    public long EventId { get; init; }
    public List<Guid> TicketIds { get; init; } = new();
    public string Reason { get; init; } = string.Empty;
}