using MassTransit;
using SharedLibrary.Tracing;

namespace BookingService.Application.Sagas;

public class BookingState : SagaStateMachineInstance, BookingStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string? CurrentState { get; set; }

    // Saga data
    public Guid BookingId { get; set; }
    public string? CustomerId { get; set; }
    public string? PaymentIntentId { get; set; }
    public int EventId { get; set; }
    public decimal TotalPrice { get; set; }
    public List<Guid> Tickets { get; set; } = new();
    public int NumberOfTickets { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}