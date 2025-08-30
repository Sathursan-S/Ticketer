using PaymentService.Domain;

namespace PaymentService.Application.Services;

public class PaymentService : IPaymentService
{
    public Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentDto dto)
    {
        // Simulate payment processing logic
        var result = new PaymentResultDto
        {
            PaymentIntentId = Guid.NewGuid().ToString(),
            Status = "Success",
            Amount = dto.Amount,
            BookingId = dto.BookingId,
            PaymentMethod = dto.PaymentMethod,
            CustomerId = dto.CustomerId
        };
        return Task.FromResult(result);
    }
}