namespace BookingService.Domain;

public class PaymentResultDto
{
    public string PaymentIntentId { get; set; }
    public Guid BookingId { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Status { get; set; }
    public decimal Amount { get; set; }
    public DateTime PayedAt { get; set; }
    public string? CustomerId { get; set; }
}