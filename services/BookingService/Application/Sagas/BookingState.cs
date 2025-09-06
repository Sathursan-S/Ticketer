using MassTransit;

namespace BookingService.Application.Sagas;

public class BookingState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string? CurrentState { get; set; }

    // Saga data
    public Guid BookingId { get; set; }
    public string? CustomerId { get; set; }
    public string? PaymentIntentId { get; set; }
    public long EventId { get; set; }
    public decimal TotalPrice { get; set; }
    public List<Guid> Tickets { get; set; } = new();
    public int NumberOfTickets { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}