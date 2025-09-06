using MassTransit;
using Microsoft.Extensions.Logging;
using TicketService.Contracts.Messages;
using TicketService.Application.Services;

namespace TicketService.Consumers;

public class CreateEventTicketConsumer : IConsumer<CreateEventTicket>
{
    private readonly ILogger<CreateEventTicketConsumer> _logger;
    private readonly ITicketService _ticketService;

    public CreateEventTicketConsumer(ILogger<CreateEventTicketConsumer> logger, ITicketService ticketService)
    {
        _logger = logger;
        _ticketService = ticketService;
    }

    public async Task Consume(ConsumeContext<CreateEventTicket> context)
    {
        var msg = context.Message;

        _logger.LogInformation("CreateEventTicket received: EventId={EventId}, NumberOfTickets={Qty}",
            msg.EventId, msg.NumberOfTickets);

        // Minimal: Process the ticket creation (e.g., create tickets for the event)
        // Assuming ITicketService has a method to create tickets
        try
        {
            var request = new DTOs.CreateTicketRequest
            {
                EventId = msg.EventId,
                Quantity = msg.NumberOfTickets
            };
            await _ticketService.CreateBulkTicketsAsync(request);
            _logger.LogInformation("Tickets created for EventId={EventId}", msg.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create tickets for EventId={EventId}", msg.EventId);
            throw; // Re-throw to let MassTransit handle retries/dead-letter
        }
    }
}
