using PaymentService.Domain;

namespace PaymentService.Application.Gateways;

/// <summary>
/// Interface for payment gateway implementations
/// Supports different payment providers (Stripe, PayPal, etc.)
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Process a payment through the gateway
    /// </summary>
    Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentDto dto);
    
    /// <summary>
    /// Refund a payment through the gateway
    /// </summary>
    Task<RefundResultDto> RefundPaymentAsync(RefundRequestDto dto);
    
    /// <summary>
    /// Verify and process webhook events from the gateway
    /// </summary>
    Task<bool> ProcessWebhookAsync(string payload, string signature);
    
    /// <summary>
    /// Gateway name for identification
    /// </summary>
    string GatewayName { get; }
}