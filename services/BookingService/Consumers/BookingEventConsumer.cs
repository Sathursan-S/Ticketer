using MassTransit;
using Microsoft.Extensions.Logging;
using SharedLibrary.Contracts.Events;

namespace BookingService.Consumers;

/// <summary>
/// Consumer for booking events
/// </summary>
public class BookingEventConsumer :
    IConsumer<BookingCreatedEvent>,
    IConsumer<BookingConfirmedEvent>,
    IConsumer<BookingFailedEvent>
{
    private readonly ILogger<BookingEventConsumer> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">Logger</param>
    public BookingEventConsumer(ILogger<BookingEventConsumer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handle BookingCreatedEvent
    /// </summary>
    /// <param name="context">Message context</param>
    public async Task Consume(ConsumeContext<BookingCreatedEvent> context)
    {
        _logger.LogInformation("Received BookingCreatedEvent: {BookingId}", context.Message.BookingId);

        // Additional processing logic can be added here

        await Task.CompletedTask;
    }

    /// <summary>
    /// Handle BookingConfirmedEvent
    /// </summary>
    /// <param name="context">Message context</param>
    public async Task Consume(ConsumeContext<BookingConfirmedEvent> context)
    {
        _logger.LogInformation("Received BookingConfirmedEvent: {BookingId}", context.Message.BookingId);

        // Additional processing logic can be added here

        await Task.CompletedTask;
    }

    /// <summary>
    /// Handle BookingFailedEvent
    /// </summary>
    /// <param name="context">Message context</param>
    public async Task Consume(ConsumeContext<BookingFailedEvent> context)
    {
        _logger.LogInformation("Received BookingFailedEvent: {BookingId}", context.Message.BookingId);

        // Additional processing logic can be added here

        await Task.CompletedTask;
    }
}
