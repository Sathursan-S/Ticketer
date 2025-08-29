namespace BookingService.Controllers;

public record HoldTicketsCommand(Guid BookingId, int EventId, int NumberOfTickets, string CustomerId);
public record ReleaseTicketsCommand(Guid BookingId, int EventId, List<Guid> TicketIds, string CustomerId);
public record ConfirmTicketsCommand(Guid BookingId, int EventId, List<Guid> TicketIds, string CustomerId);
public record ProcessPaymentCommand(Guid BookingId, decimal Amount, string PaymentMethod, string CustomerId);