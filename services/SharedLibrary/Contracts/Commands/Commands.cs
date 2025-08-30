namespace SharedLibrary.Contracts.Commands;

/// <summary>
/// Command to hold tickets for a booking
/// </summary>
public record HoldTicketsCommand
{
    public Guid BookingId { get; init; }
    public int EventId { get; init; }
    public int NumberOfTickets { get; init; }
    public string CustomerId { get; init; }
};

/// <summary>
/// Command to release previously held tickets
/// </summary>
public record ReleaseTicketsCommand(
    Guid BookingId,
    int EventId,
    IReadOnlyList<Guid> TicketIds,
    string CustomerId
);

/// <summary>
/// Command to confirm ticket reservation
/// </summary>
public record ConfirmTicketsCommand(
    Guid BookingId,
    int EventId,
    IReadOnlyList<Guid> TicketIds,
    string CustomerId
);

/// <summary>
/// Command to process payment for tickets
/// </summary>
public record ProcessPaymentCommand(
    Guid BookingId,
    decimal Amount,
    string PaymentMethod,
    string CustomerId
);

/// <summary>
/// Command to retrieve booking status
/// </summary>
public record GetStatusCommand(
    Guid BookingId,
    string CustomerId
);