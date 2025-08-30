using PaymentService.Domain;

namespace PaymentService.Application.Services;

public interface IPaymentService
{
    Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentDto dto);
    
}
