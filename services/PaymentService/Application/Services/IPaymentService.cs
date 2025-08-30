using BookingService.Domain;

namespace BookingService.Application.Services;

public interface IPaymentService
{
    Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentDto dto);
    
}
