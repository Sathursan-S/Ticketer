using MassTransit;
using SharedLibrary.Contracts.Events;
using SharedLibrary.Contracts.Messages;

namespace BookingService.Application.Sagas;

public class BookingStateMachine : MassTransitStateMachine<BookingState>
{
    private readonly ILogger<BookingStateMachine> _logger;
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

    public BookingStateMachine(ILogger<BookingStateMachine> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
            .Then(context => _logger.LogInformation("Booking created with ID: {BookingId} for Customer: {CustomerId}", context.Message.BookingId, context.Message.CustomerId))
                .Then(context =>
                {
                    context.Saga.CorrelationId = context.Message.BookingId;
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
                    CustomerId = context.Saga.CustomerId ?? string.Empty
                }))
                .Catch<Exception>(ex => ex
                    .ThenAsync(async context =>
                    {
                        var exceptionMessage = context.Exception.Message;
                        _logger.LogError(context.Exception, "An error occurred while creating booking with ID: {BookingId}", context.Saga.BookingId);
                        await context.Publish(new BookingFailedEvent
                        {
                            BookingId = context.Saga.BookingId,
                            Reason = $"An error occurred while creating booking: {exceptionMessage}"
                        });
                    })
                    .TransitionTo(BookingFailed)
                    .Finalize()
                )
                .TransitionTo(BookingCreated)
                .Then(context => _logger.LogInformation("Booking process started for BookingId: {BookingId}", context.Saga.BookingId))
        );

        // When tickets are held or failed
        During(BookingCreated,
            When(TicketsReservedEvent)
            .Then(context => _logger.LogInformation("Tickets reserved for BookingId: {BookingId}, TicketIds: {TicketIds}", context.Saga.BookingId, string.Join(", ", context.Message.TicketIds)))
                .Then(context =>
                {
                    context.Saga.Tickets = context.Message.TicketIds;
                    context.Saga.TotalPrice = context.Message.TotalPrice;
                    context.Saga.UpdatedAt = DateTime.UtcNow;
                })
                .PublishAsync(context => context.Init<ProcessPayment>(new
                {
                    BookingId = context.Saga.BookingId,
                    CustomerId = context.Saga.CustomerId ?? string.Empty,
                    Amount = context.Saga.TotalPrice
                }))
                .Catch<Exception>(ex => ex
                    .ThenAsync(async context =>
                    {
                        var exceptionMessage = context.Exception.Message;
                        _logger.LogError(context.Exception, "An error occurred while processing payment for BookingId: {BookingId}", context.Saga.BookingId);
                        await context.Publish(new BookingFailedEvent
                        {
                            BookingId = context.Saga.BookingId,
                            Reason = $"An error occurred while processing payment: {exceptionMessage}"
                        });
                    })
                    .TransitionTo(BookingFailed)
                    .Finalize()
                )
                .TransitionTo(ProcessingPayment)
                .Then(context => _logger.LogInformation("Payment processing started for BookingId: {BookingId}", context.Saga.BookingId)),

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
            .Then(context => _logger.LogInformation("Payment processed for BookingId: {BookingId}, PaymentIntentId: {PaymentIntentId}", context.Saga.BookingId, context.Message.PaymentIntentId))
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
            .Catch<Exception>(ex => ex
                .ThenAsync(async context =>
                {
                var exceptionMessage = context.Exception.Message;
                _logger.LogError(context.Exception, "An error occurred while reserving tickets for BookingId: {BookingId}", context.Saga.BookingId);
                await context.Publish(new BookingFailedEvent
                {
                    BookingId = context.Saga.BookingId,
                    Reason = $"An error occurred while reserving tickets: {exceptionMessage}"
                });
                })
                .TransitionTo(BookingFailed)
                .Finalize()
            )
            .TransitionTo(ConfirmingTickets),

            When(PaymentFailedEvent)
            .Then(context => _logger.LogWarning("Payment failed for BookingId: {BookingId}", context.Saga.BookingId))
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
            .Catch<Exception>(ex => ex
                .ThenAsync(async context =>
                {
                var exceptionMessage = context.Exception.Message;
                _logger.LogError(context.Exception, "An error occurred while handling payment failure for BookingId: {BookingId}", context.Saga.BookingId);
                await context.Publish(new BookingFailedEvent
                {
                    BookingId = context.Saga.BookingId,
                    Reason = $"An error occurred while handling payment failure: {exceptionMessage}"
                });
                })
                .TransitionTo(BookingFailed)
                .Finalize()
            )
            .TransitionTo(BookingFailed)
            .Finalize()
        );

        // Confirm booking
        During(ConfirmingTickets,
            When(BookingConfirmedEvent)
            .Then(context => _logger.LogInformation("Booking confirmed for BookingId: {BookingId}", context.Saga.BookingId))
            .TransitionTo(BookingCompleted)
            .Finalize(),

            When(BookingFailedEvent)
            .Then(context => _logger.LogWarning("Booking failed for BookingId: {BookingId}", context.Saga.BookingId))
            .TransitionTo(BookingFailed)
            .Finalize(),

            // Add handler for duplicate/delayed PaymentProcessedEvent
            When(PaymentProcessedEvent)
                .Then(context => _logger.LogInformation("Received duplicate or delayed PaymentProcessedEvent for BookingId: {BookingId} in ConfirmingTickets state. Ignoring as payment is already processed.", context.Saga.BookingId)),

            // Add new handler for PaymentFailedEvent to handle delayed messages
            When(PaymentFailedEvent)
                .Then(context => _logger.LogWarning("Received unexpected PaymentFailedEvent for BookingId: {BookingId} in ConfirmingTickets state. Handling as a failure.", context.Saga.BookingId))
                .PublishAsync(context => context.Init<ReleaseTickets>(new
                {
                    BookingId = context.Saga.BookingId,
                    EventId = context.Saga.EventId,
                    TicketIds = context.Saga.Tickets,
                    Reason = "Payment failed after tickets were reserved."
                }))
                .PublishAsync(context => context.Init<BookingFailedEvent>(new
                {
                    BookingId = context.Saga.BookingId,
                    Reason = "Payment failed after tickets were reserved."
                }))
                .TransitionTo(BookingFailed)
                .Finalize()
        );

        SetCompletedWhenFinalized();
    }
}
