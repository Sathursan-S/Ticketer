using MassTransit;
using SharedLibrary.Contracts.Events;
using SharedLibrary.Contracts.Messages;

namespace BookingService.Application.Sagas;

public class BookingStateMachine : MassTransitStateMachine<BookingState>
{
    private readonly ILogger<BookingStateMachine> _logger;

    // States
    public State Created { get; private set; }
    public State Paid { get; private set; }
    public State Confirmed { get; private set; }
    public State Failed { get; private set; }

    // Events
    public Event<BookingCreatedEvent> BookingCreated { get; private set; }
    public Event<TicketsReservedEvent> TicketsReserved { get; private set; }
    public Event<PaymentProcessedEvent> PaymentProcessed { get; private set; }
    public Event<PaymentFailedEvent> PaymentFailed { get; private set; }
    public Event<BookingFailedEvent> BookingFailed { get; private set; }

    public BookingStateMachine(ILogger<BookingStateMachine> logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);

        Event(() => BookingCreated, x => x.CorrelateById(m => m.Message.BookingId));
        Event(() => TicketsReserved, x => x.CorrelateById(m => m.Message.BookingId));
        Event(() => PaymentProcessed, x => x.CorrelateById(m => m.Message.BookingId));
        Event(() => PaymentFailed, x => x.CorrelateById(m => m.Message.BookingId));
        Event(() => BookingFailed, x => x.CorrelateById(m => m.Message.BookingId));

        // Start saga when booking is created
        Initially(
            When(BookingCreated)
                .Then(c =>
                {
                    c.Saga.CorrelationId = c.Message.BookingId;
                    c.Saga.BookingId = c.Message.BookingId;
                    c.Saga.CustomerId = c.Message.CustomerId;
                    c.Saga.EventId = c.Message.EventId;
                    c.Saga.NumberOfTickets = c.Message.NumberOfTickets;
                    c.Saga.CreatedAt = c.Message.CreatedAt;

                    _logger.LogInformation("Booking created: {BookingId} for Customer {CustomerId}",
                        c.Saga.BookingId, c.Saga.CustomerId);
                })
                .PublishAsync(c => c.Init<HoldTickets>(new
                {
                    BookingId = c.Saga.BookingId,
                    EventId = c.Saga.EventId,
                    NumberOfTickets = c.Saga.NumberOfTickets,
                    CustomerId = c.Saga.CustomerId
                }))
                .Then(c => _logger.LogInformation("HoldTickets command published for BookingId: {BookingId}", c.Saga.BookingId))
                .TransitionTo(Created)
        );

        During(Created,
            When(TicketsReserved)
                .Then(c =>
                {
                    _logger.LogInformation("TicketsReserved event received : {Message} Tickets: {Tickets}", c.Message, c.Message.TicketIds);
                    c.Saga.Tickets = c.Message.TicketIds;
                    c.Saga.TotalPrice = c.Message.TotalPrice;
                    c.Saga.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation("Tickets reserved for BookingId: {BookingId} Tickets: {Tickets}", c.Saga.BookingId, c.Saga.Tickets);
                })
                .PublishAsync(c => c.Init<ProcessPayment>(new
                {
                    BookingId = c.Saga.BookingId,
                    CustomerId = c.Saga.CustomerId ?? String.Empty,
                    Amount = c.Saga.TotalPrice
                }))
                .Then(c => _logger.LogInformation("ProcessPayment command published for BookingId: {BookingId}", c.Saga.BookingId))
                .TransitionTo(Paid),

            When(BookingFailed)
                .Then(c => _logger.LogWarning("Booking failed at Created state for BookingId: {BookingId}, Reason: {Reason}",
                    c.Saga.BookingId, c.Message.Reason))
                .Finalize()
        );

        During(Paid,
        When(PaymentFailed)
                .Then(c => _logger.LogWarning("Payment failed for BookingId: {BookingId}", c.Saga.BookingId))
                .PublishAsync(c => c.Init<ReleaseTickets>(new
                {
                    BookingID = c.Saga.BookingId,
                    EventId = c.Saga.EventId,
                    TicketIds = c.Saga.Tickets,
                    Reason = "Payment failed"
                }))
                .Then(c => _logger.LogInformation("ReleaseTickets command published for BookingId: {BookingId}", c.Saga.BookingId))
                .PublishAsync(c => c.Init<BookingFailedEvent>(new
                {
                    BookingId = c.Saga.BookingId,
                    Reason = "Payment failed"
                }))
                .Then(c => _logger.LogWarning("Booking failed event published for BookingId: {BookingId}", c.Saga.BookingId))
                .TransitionTo(Failed)
                .Finalize(),

            When(PaymentProcessed)
                .Then(c => _logger.LogInformation("Payment processed for BookingId: {BookingId} paymentIntentId: {PayId}", c.Saga.BookingId, c.Message.PaymentIntentId))
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
                .Then(c => _logger.LogInformation("ReserveTickets command published for BookingId: {BookingId}", c.Saga.BookingId))
                .TransitionTo(Confirmed)


        );

        During(Confirmed,
            When(BookingFailed)
                .Then(c => _logger.LogWarning("Booking failed at Confirmed state for BookingId: {BookingId}, Reason: {Reason}",
                    c.Saga.BookingId, c.Message.Reason))
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }
}
