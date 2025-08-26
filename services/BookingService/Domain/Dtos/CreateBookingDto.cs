namespace BookingService.Domain.Dtos;

public class CreateBookingDto
{
    public required string UserId { get; set; }
    public int EventId { get; set; }
    public required List<Guid> TicketIds { get; set; }
}
