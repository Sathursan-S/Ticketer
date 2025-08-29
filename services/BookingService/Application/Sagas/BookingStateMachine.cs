using MassTransit;
using SharedLibrary.Contracts.Messages;

namespace BookingService.Controllers;

public class BookingStateMachine : MassTransitStateMachine<BookingState>
{
    // states
    public State BookingCreated { get; private set; }
    public State TicketsHolded { get; private set; }
    public State ProcessingPayment { get; private set; }
    public State ConfirmingTickets { get; private set; }
    public State BookingCompleted { get; private set; }
    public State BookingFailed { get; private set; }

    // events
    public Event<BookingCreatedEvent> BookingCreatedEvent { get; private set; }
    public Event<PaymentProcessedEvent> PaymentProcessedEvent { get; private set; }
    public Event<BookingFailedEvent> BookingFailedEvent { get; private set; }
    public Event<BookingConfirmedEvent> BookingConfirmedEvent { get; private set; }
    public Event<TicketsReservedEvent> TicketsReservedEvent { get; private set; }
    public Event<TicketReservationFailedEvent> TicketReservationFailedEvent { get; private set; }
    public Event<PaymentFailedEvent> PaymentFailedEvent { get; private set; }

    public BookingStateMachine()
    {
        // map state to database
        InstanceState(x => x.CurrentState);

        // correlate events to state instance
        Event(() => BookingCreatedEvent, x => x.CorrelateById(context => context.Message.BookingId));
        Event(() => PaymentProcessedEvent, x => x.CorrelateById(context => context.Message.BookingId));
        Event(() => BookingFailedEvent, x => x.CorrelateById(context => context.Message.BookingId));
        Event(() => BookingConfirmedEvent, x => x.CorrelateById(context => context.Message.BookingId));
        Event(() => TicketsReservedEvent, x => x.CorrelateById(context => context.Message.BookingId));
        Event(() => TicketReservationFailedEvent, x => x.CorrelateById(context => context.Message.BookingId));
        Event(() => PaymentFailedEvent, x => x.CorrelateById(context => context.Message.BookingId));

        // Initialize transaction
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
                    NumberOfTickets = context.Saga.NumberOfTickets
                }))
                .TransitionTo(BookingCreated)
        );
        // After booking is created, hold tickets
        During(BookingCreated,
            When(TicketsReservedEvent)
                .Then<BookingState, TicketsReservedEvent>(context =>
                {
                    context.Saga.Tickets = context.Message.TicketIds;
                    context.Saga.TotalPrice = context.Message.TotalPrice;
                })
                .TransitionTo<BookingState, TicketsReservedEvent>(TicketsHolded)
                .PublishAsync(context => context.Init<ProcessPayment>(new
                {
                    BookingId = context.Saga.BookingId,
                    CustomerId = context.Saga.CustomerId,
                    TotalPrice = context.Saga.TotalPrice
                }))
                .TransitionTo<BookingState, TicketsReservedEvent>(ProcessingPayment),
            When(TicketReservationFailedEvent)
                .TransitionTo<BookingState, TicketReservationFailedEvent>(BookingFailed)
                .Finalize()
        );
        // After tickets are holded, process payment
        During(ProcessingPayment,
            When(PaymentProcessedEvent)
                .Then<BookingState, PaymentProcessedEvent>(context =>
                    {
                        context.Saga.PaymentIntentId = context.Saga.PaymentIntentId;
                    }
                )
                .PublishAsync(context => context.Init<ReserveTickets>(new
                {
                    BookingId = context.Saga.BookingId,
                    EventId = context.Saga.EventId,
                    TicketIds = context.Saga.Tickets,
                    CustomerId = context.Saga.CustomerId,
                }))
                .TransitionTo<BookingState, PaymentProcessedEvent>(ConfirmingTickets),
            When(PaymentFailedEvent)
                .PublishAsync(context => context.Init<ReleseTickets>(new
                {
                    BookingId = context.Saga.BookingId,
                    EventId = context.Saga.EventId,
                    TicketIds = context.Saga.Tickets,
                    Reason = "Payment failed"
                }))
                .TransitionTo<BookingState, PaymentFailedEvent>(BookingFailed)
                .Finalize()
        );
    }
}