using MassTransit;
using Microsoft.Extensions.Logging;
using TicketService.Contracts.Messages;
using TicketService.Domain.Dtos;    // adjust to your DTO namespace
using TicketService.Application;    // where ITicketService lives

namespace TicketService.Consumers;

public class EventCreatedConsumer : IConsumer<EventCreated>
{
    private readonly ILogger<EventCreatedConsumer> _logger;
    private readonly ITicketService _ticketService;

    public EventCreatedConsumer(ILogger<EventCreatedConsumer> logger, ITicketService ticketService)
    {
        _logger = logger;
        _ticketService = ticketService;
    }

    public async Task Consumer(ConsumeContext<EventCreated> context)
    {
        var msg = context.Message;

        _logger.LogInformation("EventCreated received: EventId={EventId}, InitialTickets={Qty}",
            msg.EventId, msg.InitialTickets);

        var request = new CreateTicketRequest
        {
            EventId  = msg.EventId,
            Quantity = msg.InitialTickets
        };

        if (request.Quantity <= 0)
        {
            _logger.LogWarning("Skip bulk creation for EventId={EventId}: quantity <= 0", msg.EventId);
            return;
        }

        try
        {
            var created = await _ticketService.CreateBulkTicketsAsync(request);
            _logger.LogInformation("Created {Count} tickets for EventId={EventId}", created, msg.EventId);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid bulk creation for EventId={EventId}", msg.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk ticket creation failed for EventId={EventId}", msg.EventId);
        }
    }
}