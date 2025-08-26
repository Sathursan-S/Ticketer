namespace TicketService.DTOs;

public class BulkCreationResponse
{
    public long EventId { get; set; }
    public int TicketsCreated { get; set; }
    public string Message { get; set; } = string.Empty;
}
