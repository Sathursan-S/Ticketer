using PaymentService.Domain;
using MassTransit;
using SharedLibrary.Contracts.Events;
using SharedLibrary.Contracts.Messages;
using PaymentService.Application.Services;

namespace PaymentService.Application.Consumers;

public class ProcessPaymentConsumer : IConsumer<ProcessPayment>
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<ProcessPaymentConsumer> _logger;

    public ProcessPaymentConsumer(IPaymentService paymentService, ILogger<ProcessPaymentConsumer> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProcessPayment> context)
    {
        var message = context.Message;

        try
        {
            _logger.LogInformation(
                "Processing payment for BookingId: {BookingId}, CustomerId: {CustomerId}, Amount: {Amount}, PaymentMethod: {PaymentMethod}",
                message.BookingId, message.CustomerId, message.Amount, message.PaymentMethod);

            var processPaymentRequest = new ProcessPaymentDto
            {
                BookingId = message.BookingId,
                CustomerId = message.CustomerId,
                Amount = message.Amount,
                PaymentMethod = message.PaymentMethod
            };

            var paymentResult = await _paymentService.ProcessPaymentAsync(processPaymentRequest);

            if (paymentResult.IsSuccess)
            {
                _logger.LogInformation("Payment processed successfully for BookingId: {BookingId}", message.BookingId);

                await context.Publish(new PaymentProcessedEvent
                {
                    BookingId = message.BookingId,
                    CustomerId = message.CustomerId,
                    Amount = paymentResult.Amount,
                    PaymentMethod = message.PaymentMethod,
                    PaymentIntentId = paymentResult.PaymentIntentId
                });
            }
            else
            {
                _logger.LogWarning("Payment failed for BookingId: {BookingId}, Reason: {Reason}", 
                    message.BookingId, paymentResult.ErrorMessage);

                await context.Publish(new PaymentFailedEvent
                {
                    BookingId = message.BookingId,
                    PaymentIntentId = paymentResult.PaymentIntentId,
                    Reason = paymentResult.ErrorMessage ?? "Payment processing failed"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while processing payment for BookingId: {BookingId}", message.BookingId);

            await context.Publish(new PaymentFailedEvent
            {
                BookingId = message.BookingId,
                Reason = $"Unexpected error: {ex.Message}"
            });

            // You may rethrow if you want MassTransit retry policy to kick in
            throw;
        }
    }
}
