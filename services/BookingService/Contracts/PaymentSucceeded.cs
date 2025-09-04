namespace BookingService.Contracts.Public;

public record PaymentSucceeded(
    Guid BookingId,
    Guid CustomerId,
    string PaymentIntentId,
    decimal Amount,
    string Currency,
    DateTime PaidAtUtc);