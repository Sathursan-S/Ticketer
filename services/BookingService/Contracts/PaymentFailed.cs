namespace BookingService.Contracts.Public;

public record PaymentFailed(
    Guid BookingId,
    Guid CustomerId,
    string Reason,
    DateTime FailedAtUtc);