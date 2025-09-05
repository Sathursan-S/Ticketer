using PaymentService.Domain;
using MassTransit;
using SharedLibrary.Contracts.Events;
using SharedLibrary.Contracts.Messages;
using PaymentService.Application.Services;
using SharedLibrary.Tracing;
using System.Diagnostics;


namespace PaymentService.Application.Consumers;

public class ProcessPaymentConsumer(
    IPaymentService _paymentService,
    ILogger<ProcessPaymentConsumer> _logger
    ) : IConsumer<ProcessPayment>
{
    public async Task Consume(ConsumeContext<ProcessPayment> context)
    {
        using var activity = TicketerTelemetry.PaymentActivitySource.StartActivity("process.payment");
        activity?.SetTag(TicketerTelemetry.CommonTags.BookingId, context.Message.BookingId.ToString());
        activity?.SetTag(TicketerTelemetry.CommonTags.UserId, context.Message.CustomerId);
        activity?.SetTag("payment.amount", context.Message.Amount);
        activity?.SetTag("payment.method", context.Message.PaymentMethod);
        activity?.SetTag(TicketerTelemetry.CommonTags.CorrelationId, context.CorrelationId?.ToString());
        
        try
        {
            _logger.LogInformation("Processing payment for BookingId: {BookingId}, CustomerId: {CustomerId}, Amount: {Amount}, PaymentMethod: {PaymentMethod}",
                context.Message.BookingId, context.Message.CustomerId, context.Message.Amount, context.Message.PaymentMethod);

            ProcessPaymentDto processPaymentRequest = new ProcessPaymentDto
            {
                BookingId = context.Message.BookingId,
                CustomerId = context.Message.CustomerId,
                Amount = context.Message.Amount,
                PaymentMethod = context.Message.PaymentMethod
            };

            PaymentResultDto paymentResult = await _paymentService.ProcessPaymentAsync(processPaymentRequest);

            if (paymentResult.IsSuccess)
            {
                activity?.SetTag("payment.result", "success");
                activity?.SetTag("payment.intent_id", paymentResult.PaymentIntentId);
                
                _logger.LogInformation("Payment processed successfully for BookingId: {BookingId}", context.Message.BookingId);
                await context.Publish(new PaymentProcessedEvent
                {
                    BookingId = context.Message.BookingId,
                    CustomerId = context.Message.CustomerId,
                    Amount = paymentResult.Amount,
                    PaymentMethod = context.Message.PaymentMethod,
                    PaymentIntentId = paymentResult.PaymentIntentId
                });
                
                // Record payment processed metric
                TicketerTelemetry.PaymentsProcessedCounter.Add(1, new TagList
                {
                    {"booking.id", context.Message.BookingId.ToString()},
                    {"payment.method", context.Message.PaymentMethod},
                    {"payment.result", "success"}
                });
            }
            else
            {
                activity?.SetTag("payment.result", "failed");
                activity?.SetTag("payment.failure_reason", paymentResult.ErrorMessage);
                activity?.SetStatus(ActivityStatusCode.Error, paymentResult.ErrorMessage);
                
                _logger.LogWarning("Payment processing failed for BookingId: {BookingId}", context.Message.BookingId);
                await context.Publish(new PaymentFailedEvent
                {
                    BookingId = context.Message.BookingId,
                    PaymentIntentId = paymentResult.PaymentIntentId,
                    Reason = paymentResult.ErrorMessage ?? "Payment processing failed"
                });
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while processing payment for BookingId: {BookingId}", context.Message.BookingId);

            await context.Publish(new PaymentFailedEvent
            {
                BookingId = context.Message.BookingId,
                Reason = $"An error occurred while processing payment: {e.Message}"
            });

            throw;
        }
    }
}