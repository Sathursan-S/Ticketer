namespace SharedLibrary.Contracts.Events;

/// <summary>
/// Represents an event that is triggered when a booking is created.
/// </summary>
public record BookingCreatedEvent
{
    public Guid BookingId { get; init; }
    public string? CustomerId { get; init; }
    public int EventId { get; init; }
    public int NumberOfTickets { get; init; }
    public DateTime CreatedAt { get; init; }
};

/// <summary>
/// Represents an event that is triggered when a booking is confirmed.
/// </summary>
/// <param name="BookingId">The unique identifier of the booking.</param>
/// <param name="CustomerId">The identifier of the customer who made the booking.</param>
/// <param name="EventId">The identifier of the event for which the booking was made.</param>
/// <param name="NumberOfTickets">The number of tickets booked.</param>
/// <param name="CreatedAt">The date and time when the booking was created.</param>
/// <param name="BookingConfirmedAt">The date and time when the booking was confirmed.</param>
public record BookingConfirmedEvent(
    Guid BookingId,
    string CustomerId,
    int EventId,
    int NumberOfTickets,
    DateTime CreatedAt,
    DateTime? BookingConfirmedAt);

/// <summary>
/// Represents an event that is triggered when a booking fails.
/// </summary>
/// <param name="BookingId">The unique identifier of the booking.</param>
/// <param name="Reason">The reason for the booking failure.</param>
public record BookingFailedEvent(
    Guid BookingId,
    string Reason);


/// <summary>
/// Represents an event that is triggered when a ticket is reserved.
/// </summary>
/// <param name="BookingId">The unique identifier of the booking.</param>
/// <param name="EventId">The identifier of the event for which the tickets are reserved.</param>
/// <param name="TicketIds">The list of unique identifiers for the reserved tickets.</param>
/// <param name="NumberOfTickets">The number of tickets reserved.</param>
/// <param name="TotalPrice">The total price of the reserved tickets.</param>
/// <param name="BookingDate">The date and time when the tickets were reserved.</param>
public record TicketsReservedEvent(
    Guid BookingId,
    int EventId,
    List<Guid> TicketIds,
    int NumberOfTickets,
    decimal TotalPrice,
    DateTime BookingDate);

/// <summary>
/// Represents an event that is triggered when a ticket reservation fails.
/// </summary>
/// <param name="BookingId">The unique identifier of the booking.</param>
/// <param name="Reason">The reason for the ticket reservation failure.</param>
public record TicketReservationFailedEvent(
    Guid BookingId,
    string? Reason);


/// <summary>
/// Represents an event that is triggered when a payment is processed.
/// </summary>
public record PaymentProcessedEvent
{
    public Guid BookingId { get; init; }
    public string? PaymentIntentId { get; init; }
    public string? CustomerId { get; init; }
    public decimal Amount { get; init; }
    public string? PaymentMethod { get; init; }
};


/// <summary>
/// Represents an event that is triggered when a payment fails.
/// </summary>
public record PaymentFailedEvent
{
    public Guid BookingId { get; init; }
    public string? PaymentIntentId { get; init; }
    public string? Reason { get; init; }
};