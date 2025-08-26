using TicketService.DTOs;

namespace TicketService.Services;

public interface ITicketService
{
    Task<IEnumerable<TicketResponse>> GetAllTicketsAsync();
    Task<TicketResponse?> GetTicketByIdAsync(Guid ticketId);
    Task<IEnumerable<TicketResponse>> GetTicketsByEventIdAsync(long eventId);
    Task<TicketResponse> CreateTicketAsync(CreateTicketRequest request);
    Task<TicketStatusResponse?> UpdateTicketStatusAsync(Guid ticketId, TicketStatus status);
    Task<bool> DeleteTicketAsync(Guid ticketId);
    Task<int> CreateBulkTicketsAsync(CreateTicketRequest request);
}
