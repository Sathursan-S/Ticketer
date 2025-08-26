namespace BookingService.Domain.Dtos;

public class BookingDto
{
    public Guid BookingId { get; set; }
    public string UserId { get; set; }
    public int EventId { get; set; }
    public List<Guid> TicketIds { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
