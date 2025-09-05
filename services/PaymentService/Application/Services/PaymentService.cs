using PaymentService.Domain;

namespace PaymentService.Application.Services;

public class PaymentService(
    ILogger<PaymentService> _logger
) : IPaymentService
{
    public Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentDto dto)
    {
        _logger.LogInformation("Processing payment for BookingId: {BookingId}, Amount: {Amount}, PaymentMethod: {PaymentMethod}",
            dto.BookingId, dto.Amount, dto.PaymentMethod);  
        // Simulate payment processing logic
        var result = new PaymentResultDto
        {
            PaymentIntentId = Guid.NewGuid().ToString(),
            Status = "Success",
            Amount = dto.Amount,
            BookingId = dto.BookingId,
            PaymentMethod = dto.PaymentMethod,
            CustomerId = dto.CustomerId,
            IsSuccess = true,
        };
        return Task.FromResult(result);
    }
}