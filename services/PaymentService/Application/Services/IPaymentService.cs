using PaymentService.Domain;

namespace PaymentService.Application.Services;

public interface IPaymentService
{
    Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentDto dto);
    Task<RefundResultDto> RefundPaymentAsync(RefundRequestDto dto);
    Task<bool> ProcessWebhookAsync(string payload, string signature, string gatewayName);
}
