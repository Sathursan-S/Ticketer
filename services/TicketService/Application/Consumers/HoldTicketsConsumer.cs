using MassTransit;
using SharedLibrary.Contracts.Events;
using SharedLibrary.Contracts.Messages;
using SharedLibrary.Tracing;
using TicketService.Application.Services;
using System.Diagnostics;

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
        using var activity = TicketerTelemetry.TicketActivitySource.StartActivity("hold.tickets");
        activity?.SetTag(TicketerTelemetry.CommonTags.BookingId, context.Message.BookingId.ToString());
        activity?.SetTag(TicketerTelemetry.CommonTags.EventId, context.Message.EventId.ToString());
        activity?.SetTag("tickets.requested", context.Message.NumberOfTickets);
        activity?.SetTag(TicketerTelemetry.CommonTags.CorrelationId, context.CorrelationId?.ToString());
        
        try
        {
            _logger.LogInformation("HoldTicketsConsumer received a message: BookingId={BookingId}, EventId={EventId}, NumberOfTickets={NumberOfTickets}",
                context.Message.BookingId, context.Message.EventId, context.Message.NumberOfTickets);
            
            // Call the ticket service to attempt holding tickets
            var result = await _ticketService.HoldTicketsAsync(context.Message);
            
            if (result.Status == DTOs.TicketHoldStatus.SUCCESS)
            {
                activity?.SetTag("operation.result", "success");
                activity?.SetTag("tickets.held", result.TicketIds.Count);
                
                _logger.LogInformation(
                    "Successfully held {Count} tickets for event {EventId} for booking {BookingId}",
                    result.TicketIds.Count, result.EventId, context.Message.BookingId);
                
                // TODO: move price cal inside service
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
                
                // Record success metrics
                TicketerTelemetry.TicketsReservedCounter.Add(result.TicketIds.Count, new TagList
                {
                    {"event.id", context.Message.EventId.ToString()},
                    {"booking.id", context.Message.BookingId.ToString()}
                });
            }
            else
            {
                activity?.SetTag("operation.result", "failed");
                activity?.SetTag("failure.reason", result.ErrorMessage);
                activity?.SetStatus(ActivityStatusCode.Error, result.ErrorMessage);
                
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
            activity?.SetTag("operation.result", "error");
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
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