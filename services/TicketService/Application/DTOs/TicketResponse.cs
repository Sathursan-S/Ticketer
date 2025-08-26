namespace TicketService.DTOs;

public class TicketResponse
{
    public Guid TicketId {get; set;}
    public long EventId {get; set;}
    public TicketStatus Status {get; set;}
}