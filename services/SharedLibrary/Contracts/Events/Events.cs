namespace BookingService.Controllers;

public record BookingCreatedEvent(Guid BookingId, string CustomerId, int EventID, int Quantity, DateTime BookingDate);
public record TicketReservedEvent(Guid BookingId, int EventID, List<Guid> TicketIds, int Quantity, DateTime BookingDate);
public record PaymentProcessedEvent(Guid BookingId, Guid PaymentIntentId, string UserId, decimal Amount, string PaymentStatus);
public record BookingFailedEvent(Guid BookingId, string Reason); 
public record BookingConfirmedEvent(Guid BookingId, string CustomerId, int EventID,List<Guid> TicketIds, int Quantity, DateTime BookingDate);
public record TicketsReservedEvent(Guid BookingId, int EventID, List<Guid> TicketIds, int Quantity, DateTime BookingDate);
public record TicketReservationFailedEvent(Guid BookingId, string Reason);
public record PaymentFailedEvent(Guid BookingId, string Reason);