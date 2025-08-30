using MassTransit;
using SharedLibrary.Contracts.Events;
using SharedLibrary.Contracts.Messages;
using TicketService.Application.Services;

namespace TicketService.Application.Consumers;

public class HoldTicketsConsumer : IConsumer<HoldTickets>
{
    private readonly ITicketService _ticketService;
    private readonly ILogger<HoldTicketsConsumer> _logger;

    public HoldTicketsConsumer(ITicketService ticketService, ILogger<HoldTicketsConsumer> logger)
    {
        _ticketService = ticketService ?? throw new ArgumentNullException(nameof(ticketService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Consume(ConsumeContext<HoldTickets> context)
    {
        try
        {
            _logger.LogInformation("HoldTicketsConsumer received a message: BookingId={BookingId}, EventId={EventId}, NumberOfTickets={NumberOfTickets}",
                context.Message.BookingId, context.Message.EventId, context.Message.NumberOfTickets);
            
            // Call the ticket service to attempt holding tickets
            var result = await _ticketService.HoldTicketsAsync(context.Message);
            
            if (result.Status == DTOs.TicketHoldStatus.SUCCESS)
            {
                _logger.LogInformation(
                    "Successfully held {Count} tickets for event {EventId} for booking {BookingId}",
                    result.TicketIds.Count, result.EventId, context.Message.BookingId);
                
                // Calculate total price (this would typically be based on actual ticket prices from the database)
                decimal totalPrice = result.TicketIds.Count * 25.0m; // Simplified - $25 per ticket
                
                // Publish success event with ticket IDs
                await context.Publish(new TicketsReservedEvent(
                    BookingId: context.Message.BookingId,
                    EventId: (int)result.EventId,
                    TicketIds: result.TicketIds,
                    NumberOfTickets: result.TicketIds.Count,
                    TotalPrice: totalPrice,
                    BookingDate: DateTime.UtcNow
                ));
            }
            else
            {
                _logger.LogWarning(
                    "Failed to hold tickets for event {EventId} for booking {BookingId}: {ErrorMessage}",
                    result.EventId, context.Message.BookingId, result.ErrorMessage);
                
                // Publish failure event
                await context.Publish(new TicketReservationFailedEvent(
                    BookingId: context.Message.BookingId,
                    Reason: result.ErrorMessage ?? "Failed to reserve tickets"
                ));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing HoldTickets message for booking {BookingId}", context.Message.BookingId);
            
            // Publish failure event in case of exception
            await context.Publish(new TicketReservationFailedEvent(
                BookingId: context.Message.BookingId,
                Reason: $"An unexpected error occurred: {ex.Message}"
            ));
            
            // Re-throw to ensure the message is not acknowledged
            throw;
        }
    }
}