namespace TicketService.DTOs;

public class TicketStatusResponse
{
    public long EventId { get; set; }
    public Guid TicketId { get; set; }
    public TicketStatus Status { get; set; }
}