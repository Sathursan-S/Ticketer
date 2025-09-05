using MassTransit;
using SharedLibrary.Contracts.Events;
using SharedLibrary.Contracts.Messages;
using TicketService.Application.Services;

namespace TicketService.Application.Consumers;

public class ReserveTicketsConsumer : IConsumer<ReserveTickets>
{
    private readonly ITicketService _ticketService;
    private readonly ILogger<ReserveTicketsConsumer> _logger;

    public ReserveTicketsConsumer(ITicketService ticketService, ILogger<ReserveTicketsConsumer> logger)
    {
        _ticketService = ticketService ?? throw new ArgumentNullException(nameof(ticketService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<ReserveTickets> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "ReserveTicketsConsumer received a message: BookingId={BookingId}, TicketIds={TicketIds}",
            message.BookingId, string.Join(", ", message.TicketIds));

        try
        {
            bool result = await _ticketService.ReserveTicketsAsync(message);

            if (result)
            {
                _logger.LogInformation(
                    "Successfully reserved tickets for BookingId={BookingId}", message.BookingId);
                
                // Publish booking confirmed event
                await context.Publish(new BookingConfirmedEvent(
                    message.BookingId,
                    message.CustomerId,
                    message.EventId,
                    message.TicketIds.Count,
                    DateTime.UtcNow,
                    DateTime.UtcNow
                ));
            }
            else
            {
                _logger.LogWarning(
                    "Failed to reserve tickets for BookingId={BookingId}", message.BookingId);
                
                // Publish booking failed event
                await context.Publish(new BookingFailedEvent
                {
                    BookingId = message.BookingId,
                    Reason = "Failed to reserve tickets",
                    EventId = message.EventId
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while reserving tickets for BookingId={BookingId}", message.BookingId);
            
            // Publish booking failed event
            await context.Publish(new BookingFailedEvent
            {
                BookingId = message.BookingId,
                Reason = $"Exception occurred while reserving tickets: {ex.Message}",
                EventId = message.EventId
            });
        }
    }
}