namespace TicketService.DTOs;

public class HoldTicketResponse
{
    public long EventId {get; set;}
    public List<Guid> TicketIds {get; set;}
    public TicketHoldStatus Status {get; set;}
    public string? ErrorMessage {get; set;}
    
}

public enum TicketHoldStatus
{
    SUCCESS,
    FAILED
}