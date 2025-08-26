namespace TicketService.DTOs;

public class CreateTicketRequest
{
    public long EventId { get; set; }
    public int Quantity { get; set; }
}