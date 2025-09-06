using TicketService.DTOs;
using TicketService;

namespace TicketService.Repositoy;

public interface ITicketRepository
{
    Task<IEnumerable<Ticket>> GetTicketsAsync();
    Task<Ticket?> GetTicketByIdAsync(Guid ticketId);
    Task<IEnumerable<Ticket>> GetTicketsByEventIdAsync(long eventId);
    Task<Ticket> CreateTicketAsync(Ticket request);
    Task<bool> UpdateTicketStatusAsync(Guid ticketId, TicketStatus status);
    Task<bool> ReserveTicketsAsync(IEnumerable<Guid> ticketIds, long eventId);
    Task<bool> ReleaseTicketsAsync(IEnumerable<Guid> ticketIds, long eventId);
    Task<bool> DeleteTicketAsync(Guid ticketId);
}