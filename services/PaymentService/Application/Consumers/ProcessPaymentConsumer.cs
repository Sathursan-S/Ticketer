using BookingService.Domain;
using MassTransit;
using SharedLibrary.Contracts.Events;
using SharedLibrary.Contracts.Messages;
using BookingService.Application.Services;


namespace PaymentService.Application.Consumers;

public class ProcessPaymentConsumer(
    IPaymentService _paymentService,
    ILogger<ProcessPaymentConsumer> _logger) : IConsumer<ProcessPayment>
{
    public async Task Consume(ConsumeContext<ProcessPayment> context)
    {
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
                _logger.LogInformation("Payment processed successfully for BookingId: {BookingId}", context.Message.BookingId);
                await context.Publish(new PaymentProcessedEvent
                {
                    BookingId = context.Message.BookingId,
                    CustomerId = context.Message.CustomerId,
                    Amount = paymentResult.Amount,
                    PaymentMethod = context.Message.PaymentMethod,
                    PaymentIntentId = paymentResult.PaymentIntentId
                });
            }
            else
            {
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
            Console.WriteLine(e);
            throw;
        }
    }
}