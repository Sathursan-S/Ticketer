namespace PaymentService.Domain;

public class RefundRequestDto
{
    public string PaymentIntentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public Guid BookingId { get; set; }
}