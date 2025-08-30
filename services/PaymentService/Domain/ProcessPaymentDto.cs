namespace BookingService.Domain;

public class ProcessPaymentDto
{
    public Guid BookingId { get; set; }
    public string? CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}