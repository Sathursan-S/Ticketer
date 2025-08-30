using MassTransit;
using Microsoft.Extensions.Logging;
using SharedLibrary.Contracts.Events;
using SharedLibrary.Contracts.Messages;

namespace BookingService.Consumers;

/// <summary>
/// Consumer for handling payment-related messages
/// </summary>
public class PaymentMessageConsumer : 
    IConsumer<ProcessPayment>
{
    private readonly ILogger<PaymentMessageConsumer> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger</param>
    public PaymentMessageConsumer(ILogger<PaymentMessageConsumer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handle ProcessPayment message
    /// </summary>
    /// <param name="context">Message context</param>
    public async Task Consume(ConsumeContext<ProcessPayment> context)
    {
        var message = context.Message;
        
        _logger.LogInformation(
            "Processing payment for booking {BookingId} by customer {CustomerId}", 
            message.BookingId, message.CustomerId);
        
        // Simulating payment processing
        await Task.Delay(500); // Simulate external API call delay
        
        // In a real-world scenario, you would call a payment gateway here
        // For now, we'll just simulate a successful payment
        var success = true;
        
        if (success)
        {
            // Publishing a payment processed event
            await context.Publish<PaymentProcessedEvent>(new
            {
                message.BookingId,
                PaymentIntentId = Guid.NewGuid().ToString() // Generate a payment intent ID
            });
            
            _logger.LogInformation("Payment processed successfully for booking {BookingId}", message.BookingId);
        }
        else
        {
            // Publishing a payment failed event
            await context.Publish<PaymentFailedEvent>(new
            {
                message.BookingId,
                Reason = "Payment gateway declined the transaction"
            });
            
            _logger.LogWarning("Payment failed for booking {BookingId}", message.BookingId);
        }
    }
}
