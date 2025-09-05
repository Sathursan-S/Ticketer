using MassTransit;
using SharedLibrary.Contracts.Events;
using SharedLibrary.Contracts.Messages;
using TicketService.Application.Services;

namespace TicketService.Application.Consumers;

public class ReleaseTicketsConsumer : IConsumer<ReleaseTickets>
{
    private readonly ITicketService _ticketService;
    private readonly ILogger<ReleaseTicketsConsumer> _logger;

    public ReleaseTicketsConsumer(ITicketService ticketService, ILogger<ReleaseTicketsConsumer> logger)
    {
        _ticketService = ticketService ?? throw new ArgumentNullException(nameof(ticketService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<ReleaseTickets> context)
    {
        var message = context.Message;
        _logger.LogInformation(
            "BookingFailedConsumer received a message: BookingId={BookingId}, Reason={Reason}",
            message.BookingId, message.Reason);

        try
        {
            if (message.TicketIds == null || !message.TicketIds.Any())
            {
                _logger.LogWarning("No tickets to release for BookingId={BookingId}", message.BookingId);
                return;
            }

            var releaseTickets = new ReleaseTickets
            {
                BookingId = message.BookingId,
                EventId = message.EventId,
                TicketIds = message.TicketIds,
                Reason = message.Reason
            };

            bool result = await _ticketService.ReleaseTicketsAsync(releaseTickets);

            if (!result)
            {
                _logger.LogWarning(
                    "Failed to release held tickets for BookingId={BookingId}", message.BookingId);
            }
            else
            {
                _logger.LogInformation(
                    "Released held tickets for BookingId={BookingId}", message.BookingId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while releasing tickets for BookingId={BookingId}", message.BookingId);
            throw;
        }
    }
}