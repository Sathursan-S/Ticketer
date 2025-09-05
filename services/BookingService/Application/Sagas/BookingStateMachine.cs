using MassTransit;
using SharedLibrary.Contracts.Events;
using SharedLibrary.Contracts.Messages;
using SharedLibrary.Tracing;
using System.Diagnostics;

namespace BookingService.Application.Sagas;

public class BookingStateMachine : MassTransitStateMachine<BookingState>
{
    // States
    public State BookingCreated { get; private set; }
    public State ProcessingPayment { get; private set; }
    public State ConfirmingTickets { get; private set; }
    public State BookingCompleted { get; private set; }
    public State BookingFailed { get; private set; }

    // Events
    public Event<BookingCreatedEvent> BookingCreatedEvent { get; private set; }
    public Event<TicketsReservedEvent> TicketsReservedEvent { get; private set; }
    public Event<TicketReservationFailedEvent> TicketReservationFailedEvent { get; private set; }
    public Event<PaymentProcessedEvent> PaymentProcessedEvent { get; private set; }
    public Event<PaymentFailedEvent> PaymentFailedEvent { get; private set; }
    public Event<BookingConfirmedEvent> BookingConfirmedEvent { get; private set; }
    public Event<BookingFailedEvent> BookingFailedEvent { get; private set; }

    public BookingStateMachine()
    {
        // Map saga state to DB
        InstanceState(x => x.CurrentState);

        // Correlate events
        Event(() => BookingCreatedEvent, x => x.CorrelateById(context => context.Message.BookingId));
        Event(() => TicketsReservedEvent, x => x.CorrelateById(context => context.Message.BookingId));
        Event(() => TicketReservationFailedEvent, x => x.CorrelateById(context => context.Message.BookingId));
        Event(() => PaymentProcessedEvent, x => x.CorrelateById(context => context.Message.BookingId));
        Event(() => PaymentFailedEvent, x => x.CorrelateById(context => context.Message.BookingId));
        Event(() => BookingConfirmedEvent, x => x.CorrelateById(context => context.Message.BookingId));
        Event(() => BookingFailedEvent, x => x.CorrelateById(context => context.Message.BookingId));

        // Start saga when booking is created
        Initially(
            When(BookingCreatedEvent)
                .Then(context =>
                {
                    using var activity = context.StartSagaActivity("booking.created", context.Saga);
                    activity?.RecordStateTransition(context.Saga.CurrentState ?? "Initial", "BookingCreated");
                    
                    context.Saga.CorrelationId = context.Message.BookingId;
                    context.Saga.CustomerId = context.Message.CustomerId;
                    context.Saga.BookingId = context.Message.BookingId;
                    context.Saga.CreatedAt = DateTime.UtcNow;
                    context.Saga.EventId = context.Message.EventId;
                    context.Saga.NumberOfTickets = context.Message.NumberOfTickets;
                    
                    // Record saga started metric
                    TicketerTelemetry.SagaStartedCounter.Add(1, new TagList
                    {
                        {"saga.type", "BookingOrchestration"},
                        {"booking.id", context.Saga.BookingId.ToString()},
                        {"customer.id", context.Saga.CustomerId ?? "unknown"}
                    });
                })
                .PublishAsync(context => 
                {
                    using var activity = context.StartPublishActivity("HoldTickets");
                    return context.Init<HoldTickets>(new
                    {
                        BookingId = context.Saga.BookingId,
                        EventId = context.Saga.EventId,
                        NumberOfTickets = context.Saga.NumberOfTickets,
                        CustomerId = context.Saga.CustomerId ?? string.Empty
                    });
                })
                .TransitionTo(BookingCreated)
        );

        // When tickets are held or failed
        During(BookingCreated,
            When(TicketsReservedEvent)
                .Then(context =>
                {
                    using var activity = context.StartSagaActivity("tickets.reserved", context.Saga);
                    activity?.RecordStateTransition("BookingCreated", "ProcessingPayment");
                    
                    context.Saga.Tickets = context.Message.TicketIds;
                    context.Saga.TotalPrice = context.Message.TotalPrice;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                    
                    // Record business metric
                    TicketerTelemetry.TicketsReservedCounter.Add(context.Message.TicketIds.Count, new TagList
                    {
                        {"booking.id", context.Saga.BookingId.ToString()},
                        {"event.id", context.Saga.EventId.ToString()}
                    });
                })
                .PublishAsync(context =>
                {
                    using var activity = context.StartPublishActivity("ProcessPayment");
                    return context.Init<ProcessPayment>(new
                    {
                        BookingId = context.Saga.BookingId,
                        CustomerId = context.Saga.CustomerId ?? string.Empty,
                        Amount = context.Saga.TotalPrice
                    });
                })
                .TransitionTo(ProcessingPayment),

            When(TicketReservationFailedEvent)
                .Then(context =>
                {
                    using var activity = context.StartSagaActivity("tickets.reservation.failed", context.Saga);
                    activity?.RecordSagaFailure("Ticket reservation failed");
                    activity?.RecordStateTransition("BookingCreated", "BookingFailed");
                    
                    // Record failure metric
                    TicketerTelemetry.SagaFailedCounter.Add(1, new TagList
                    {
                        {"saga.type", "BookingOrchestration"},
                        {"failure.reason", "ticket_reservation_failed"},
                        {"booking.id", context.Saga.BookingId.ToString()}
                    });
                })
                .PublishAsync(context =>
                {
                    using var activity = context.StartPublishActivity("BookingFailedEvent");
                    return context.Init<BookingFailedEvent>(new
                    {
                        BookingId = context.Saga.BookingId,
                        Reason = "Ticket reservation failed"
                    });
                })
                .TransitionTo(BookingFailed)
                .Finalize()
        );

        // Payment processing
        During(ProcessingPayment,
            When(PaymentProcessedEvent)
                .Then(context =>
                {
                    context.Saga.PaymentIntentId = context.Message.PaymentIntentId;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .PublishAsync(context => context.Init<ReserveTickets>(new
                {
                    BookingId = context.Saga.BookingId,
                    EventId = context.Saga.EventId,
                    TicketIds = context.Saga.Tickets,
                    CustomerId = context.Saga.CustomerId ?? string.Empty
                }))
                .TransitionTo(ConfirmingTickets),

            When(PaymentFailedEvent)
                .PublishAsync(context => context.Init<ReleaseTickets>(new
                {
                    BookingId = context.Saga.BookingId,
                    EventId = context.Saga.EventId,
                    TicketIds = context.Saga.Tickets,
                    Reason = "Payment failed"
                }))
                .PublishAsync(context => context.Init<BookingFailedEvent>(new
                {
                    BookingId = context.Saga.BookingId,
                    Reason = "Payment failed"
                }))
                .TransitionTo(BookingFailed)
                .Finalize()
        );

        // Confirm booking
        During(ConfirmingTickets,
            When(BookingConfirmedEvent)
                .TransitionTo(BookingCompleted)
                .Finalize(),

            When(BookingFailedEvent)
                .TransitionTo(BookingFailed)
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }
}
