namespace PaymentService.Domain;

public class RefundResultDto
{
    public string RefundId { get; set; } = string.Empty;
    public string PaymentIntentId { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RefundedAt { get; set; }
    public Guid BookingId { get; set; }
}