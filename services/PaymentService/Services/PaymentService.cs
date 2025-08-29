namespace BookingService.Controllers;

public class PaymentService : IPaymentService
{
    public Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentDto dto)
    {
        // Simulate payment processing logic
        var result = new PaymentResultDto
        {
            PaymentId = Guid.NewGuid(),
            Status = "Success",
            Amount = dto.Amount,
            BookingId = dto.BookingId
        };
        return Task.FromResult(result);
    }
}