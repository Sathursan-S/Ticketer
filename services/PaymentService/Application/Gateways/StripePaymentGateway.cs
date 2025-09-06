using Stripe;
using PaymentService.Domain;
using PaymentService.Application.Gateways;
using Microsoft.Extensions.Options;

namespace PaymentService.Application.Gateways;

public class StripeSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}

public class StripePaymentGateway : IPaymentGateway
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripePaymentGateway> _logger;
    private readonly PaymentIntentService _paymentIntentService;
    private readonly RefundService _refundService;

    public string GatewayName => "Stripe";

    public StripePaymentGateway(IOptions<StripeSettings> settings, ILogger<StripePaymentGateway> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        
        StripeConfiguration.ApiKey = _settings.SecretKey;
        _paymentIntentService = new PaymentIntentService();
        _refundService = new RefundService();
    }

    public async Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentDto dto)
    {
        try
        {
            _logger.LogInformation("Processing Stripe payment for BookingId: {BookingId}, Amount: {Amount}", 
                dto.BookingId, dto.Amount);

            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(dto.Amount * 100), // Stripe expects amounts in cents
                Currency = "usd", // TODO: Make configurable
                PaymentMethod = dto.PaymentMethod,
                ConfirmationMethod = "manual",
                Confirm = true,
                ReturnUrl = "https://your-website.com/return", // TODO: Make configurable
                Metadata = new Dictionary<string, string>
                {
                    {"booking_id", dto.BookingId.ToString()},
                    {"customer_id", dto.CustomerId ?? ""}
                }
            };

            var paymentIntent = await _paymentIntentService.CreateAsync(options);
            
            var result = new PaymentResultDto
            {
                PaymentIntentId = paymentIntent.Id,
                Status = paymentIntent.Status,
                Amount = dto.Amount,
                BookingId = dto.BookingId,
                PaymentMethod = dto.PaymentMethod,
                CustomerId = dto.CustomerId,
                IsSuccess = paymentIntent.Status == "succeeded",
                PayedAt = DateTime.UtcNow
            };

            if (!result.IsSuccess)
            {
                result.ErrorMessage = $"Payment status: {paymentIntent.Status}";
                _logger.LogWarning("Stripe payment not successful. Status: {Status}, PaymentIntentId: {PaymentIntentId}", 
                    paymentIntent.Status, paymentIntent.Id);
            }
            else
            {
                _logger.LogInformation("Stripe payment successful. PaymentIntentId: {PaymentIntentId}", paymentIntent.Id);
            }

            return result;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe payment failed for BookingId: {BookingId}", dto.BookingId);
            return new PaymentResultDto
            {
                PaymentIntentId = "",
                BookingId = dto.BookingId,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                CustomerId = dto.CustomerId,
                IsSuccess = false,
                Status = "failed",
                ErrorMessage = ex.Message,
                PayedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing Stripe payment for BookingId: {BookingId}", dto.BookingId);
            return new PaymentResultDto
            {
                PaymentIntentId = "",
                BookingId = dto.BookingId,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                CustomerId = dto.CustomerId,
                IsSuccess = false,
                Status = "failed",
                ErrorMessage = "An unexpected error occurred",
                PayedAt = DateTime.UtcNow
            };
        }
    }

    public async Task<RefundResultDto> RefundPaymentAsync(RefundRequestDto dto)
    {
        try
        {
            _logger.LogInformation("Processing Stripe refund for PaymentIntentId: {PaymentIntentId}, Amount: {Amount}", 
                dto.PaymentIntentId, dto.Amount);

            var options = new RefundCreateOptions
            {
                PaymentIntent = dto.PaymentIntentId,
                Amount = (long)(dto.Amount * 100), // Stripe expects amounts in cents
                Reason = dto.Reason switch
                {
                    "duplicate" => "duplicate",
                    "fraudulent" => "fraudulent",
                    "requested_by_customer" => "requested_by_customer",
                    _ => "requested_by_customer"
                },
                Metadata = new Dictionary<string, string>
                {
                    {"booking_id", dto.BookingId.ToString()}
                }
            };

            var refund = await _refundService.CreateAsync(options);

            return new RefundResultDto
            {
                RefundId = refund.Id,
                PaymentIntentId = dto.PaymentIntentId,
                IsSuccess = refund.Status == "succeeded",
                Amount = dto.Amount,
                Status = refund.Status,
                BookingId = dto.BookingId,
                RefundedAt = DateTime.UtcNow,
                ErrorMessage = refund.Status != "succeeded" ? $"Refund status: {refund.Status}" : null
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe refund failed for PaymentIntentId: {PaymentIntentId}", dto.PaymentIntentId);
            return new RefundResultDto
            {
                RefundId = "",
                PaymentIntentId = dto.PaymentIntentId,
                IsSuccess = false,
                Amount = dto.Amount,
                Status = "failed",
                BookingId = dto.BookingId,
                RefundedAt = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing Stripe refund for PaymentIntentId: {PaymentIntentId}", dto.PaymentIntentId);
            return new RefundResultDto
            {
                RefundId = "",
                PaymentIntentId = dto.PaymentIntentId,
                IsSuccess = false,
                Amount = dto.Amount,
                Status = "failed",
                BookingId = dto.BookingId,
                RefundedAt = DateTime.UtcNow,
                ErrorMessage = "An unexpected error occurred"
            };
        }
    }

    public async Task<bool> ProcessWebhookAsync(string payload, string signature)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, _settings.WebhookSecret);
            
            _logger.LogInformation("Processing Stripe webhook event: {EventType}, Id: {EventId}", 
                stripeEvent.Type, stripeEvent.Id);

            switch (stripeEvent.Type)
            {
                case "payment_intent.succeeded":
                    await HandlePaymentSucceeded(stripeEvent);
                    break;
                case "payment_intent.payment_failed":
                    await HandlePaymentFailed(stripeEvent);
                    break;
                case "charge.dispute.created":
                    await HandleChargeDispute(stripeEvent);
                    break;
                default:
                    _logger.LogInformation("Unhandled Stripe webhook event type: {EventType}", stripeEvent.Type);
                    break;
            }

            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to verify Stripe webhook signature");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing Stripe webhook");
            return false;
        }
    }

    private Task HandlePaymentSucceeded(Event stripeEvent)
    {
        if (stripeEvent.Data.Object is PaymentIntent paymentIntent)
        {
            _logger.LogInformation("Payment succeeded for PaymentIntentId: {PaymentIntentId}", paymentIntent.Id);
            // TODO: Publish payment succeeded event via MassTransit
            // This would integrate with the existing messaging system
        }
        return Task.CompletedTask;
    }

    private Task HandlePaymentFailed(Event stripeEvent)
    {
        if (stripeEvent.Data.Object is PaymentIntent paymentIntent)
        {
            _logger.LogWarning("Payment failed for PaymentIntentId: {PaymentIntentId}", paymentIntent.Id);
            // TODO: Publish payment failed event via MassTransit
        }
        return Task.CompletedTask;
    }

    private Task HandleChargeDispute(Event stripeEvent)
    {
        _logger.LogWarning("Charge dispute created: {EventId}", stripeEvent.Id);
        // TODO: Handle dispute logic
        return Task.CompletedTask;
    }
}