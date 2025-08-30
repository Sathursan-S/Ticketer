using MassTransit;

namespace BookingService.Application.Sagas;

public class BookingState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; }

    public Guid BookingId { get; set; } = Guid.NewGuid();
    public string CustomerId { get; set; }
    public string? PaymentIntentId { get; set; }
    public int EventId { get; set; }
    public decimal TotalPrice { get; set; }
    public List<Guid> Tickets { get; set; } = new();
    public int NumberOfTickets { get; set; }
    public DateTime CreatedAt { get; set; }
}