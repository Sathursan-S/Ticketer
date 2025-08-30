using MassTransit;
using SharedLibrary.Contracts.Events;
using SharedLibrary.Contracts.Messages;

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
                    context.Saga.CustomerId = context.Message.CustomerId;
                    context.Saga.BookingId = context.Message.BookingId;
                    context.Saga.CreatedAt = DateTime.UtcNow;
                    context.Saga.EventId = context.Message.EventId;
                    context.Saga.NumberOfTickets = context.Message.NumberOfTickets;
                })
                .PublishAsync(context => context.Init<HoldTickets>(new
                {
                    BookingId = context.Saga.BookingId,
                    EventId = context.Saga.EventId,
                    NumberOfTickets = context.Saga.NumberOfTickets,
                    CustomerId = context.Saga.CustomerId
                }))
                .TransitionTo(BookingCreated)
        );

        // When tickets are held or failed
        During(BookingCreated,
            When(TicketsReservedEvent)
                .Then(context =>
                {
                    context.Saga.Tickets = context.Message.TicketIds;
                    context.Saga.TotalPrice = context.Message.TotalPrice;
                })
                .PublishAsync(context => context.Init<ProcessPayment>(new
                {
                    BookingId = context.Saga.BookingId,
                    CustomerId = context.Saga.CustomerId,
                    TotalPrice = context.Saga.TotalPrice
                }))
                .TransitionTo(ProcessingPayment),

            When(TicketReservationFailedEvent)
                .PublishAsync(context => context.Init<BookingFailedEvent>(new
                {
                    BookingId = context.Saga.BookingId,
                    Reason = "Ticket reservation failed"
                }))
                .TransitionTo(BookingFailed)
                .Finalize()
        );

        // Payment processing
        During(ProcessingPayment,
            When(PaymentProcessedEvent)
                .Then(context =>
                {
                    context.Saga.PaymentIntentId = context.Message.PaymentIntentId;
                })
                .PublishAsync(context => context.Init<ReserveTickets>(new
                {
                    BookingId = context.Saga.BookingId,
                    EventId = context.Saga.EventId,
                    TicketIds = context.Saga.Tickets,
                    CustomerId = context.Saga.CustomerId
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
