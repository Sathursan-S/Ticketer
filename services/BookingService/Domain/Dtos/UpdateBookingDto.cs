namespace BookingService.Domain.Dtos;

public class UpdateBookingDto
{
    public required Guid BookingId { get; set; }
    public string? UserId { get; set; }
    public int? EventId { get; set; }
    public List<Guid>? TicketIds { get; set; }
    public string? Status { get; set; }
}
