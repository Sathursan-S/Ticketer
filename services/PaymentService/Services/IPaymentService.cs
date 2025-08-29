namespace BookingService.Controllers;

public interface IPaymentService
{
    Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentDto dto);
}