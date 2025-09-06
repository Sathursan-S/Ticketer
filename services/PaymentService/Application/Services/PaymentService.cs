using PaymentService.Domain;
using PaymentService.Application.Gateways;

namespace PaymentService.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IEnumerable<IPaymentGateway> _paymentGateways;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IEnumerable<IPaymentGateway> paymentGateways, ILogger<PaymentService> logger)
    {
        _paymentGateways = paymentGateways;
        _logger = logger;
    }

    public async Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentDto dto)
    {
        _logger.LogInformation("Processing payment for BookingId: {BookingId}, Amount: {Amount}, PaymentMethod: {PaymentMethod}",
            dto.BookingId, dto.Amount, dto.PaymentMethod);

        // For now, default to Stripe gateway
        // TODO: Add logic to select gateway based on payment method or configuration
        var gateway = _paymentGateways.FirstOrDefault(g => g.GatewayName == "Stripe");
        
        if (gateway == null)
        {
            _logger.LogError("No payment gateway found for processing payment");
            return new PaymentResultDto
            {
                PaymentIntentId = "",
                Status = "failed",
                Amount = dto.Amount,
                BookingId = dto.BookingId,
                PaymentMethod = dto.PaymentMethod,
                CustomerId = dto.CustomerId,
                IsSuccess = false,
                PayedAt = DateTime.UtcNow,
                ErrorMessage = "No payment gateway available"
            };
        }

        var result = await gateway.ProcessPaymentAsync(dto);
        
        _logger.LogInformation("Payment processing completed for BookingId: {BookingId}, Success: {IsSuccess}", 
            dto.BookingId, result.IsSuccess);
            
        return result;
    }

    public async Task<RefundResultDto> RefundPaymentAsync(RefundRequestDto dto)
    {
        _logger.LogInformation("Processing refund for PaymentIntentId: {PaymentIntentId}, Amount: {Amount}",
            dto.PaymentIntentId, dto.Amount);

        // For now, default to Stripe gateway
        // TODO: Add logic to determine which gateway was used for original payment
        var gateway = _paymentGateways.FirstOrDefault(g => g.GatewayName == "Stripe");
        
        if (gateway == null)
        {
            _logger.LogError("No payment gateway found for processing refund");
            return new RefundResultDto
            {
                RefundId = "",
                PaymentIntentId = dto.PaymentIntentId,
                IsSuccess = false,
                Amount = dto.Amount,
                Status = "failed",
                BookingId = dto.BookingId,
                RefundedAt = DateTime.UtcNow,
                ErrorMessage = "No payment gateway available"
            };
        }

        var result = await gateway.RefundPaymentAsync(dto);
        
        _logger.LogInformation("Refund processing completed for PaymentIntentId: {PaymentIntentId}, Success: {IsSuccess}", 
            dto.PaymentIntentId, result.IsSuccess);
            
        return result;
    }

    public async Task<bool> ProcessWebhookAsync(string payload, string signature, string gatewayName)
    {
        _logger.LogInformation("Processing webhook for gateway: {GatewayName}", gatewayName);

        var gateway = _paymentGateways.FirstOrDefault(g => 
            g.GatewayName.Equals(gatewayName, StringComparison.OrdinalIgnoreCase));
        
        if (gateway == null)
        {
            _logger.LogWarning("No payment gateway found for webhook: {GatewayName}", gatewayName);
            return false;
        }

        var result = await gateway.ProcessWebhookAsync(payload, signature);
        
        _logger.LogInformation("Webhook processing completed for gateway: {GatewayName}, Success: {Result}", 
            gatewayName, result);
            
        return result;
    }
}