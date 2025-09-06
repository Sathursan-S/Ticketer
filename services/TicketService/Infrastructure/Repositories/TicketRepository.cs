using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TicketService.DTOs;
using TicketService.Repositoy;

namespace TicketService.Repository;

public class TicketRepository : ITicketRepository
{
    private readonly TicketDbContext _context;
    private readonly ILogger<TicketRepository> _logger;

    public TicketRepository(TicketDbContext context, ILogger<TicketRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<Ticket>> GetTicketsAsync()
    {
        try
        {
            _logger.LogInformation("Getting all tickets");
            return await _context.Tickets.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all tickets");
            throw new RepositoryException("Failed to retrieve tickets", ex);
        }
    }

    public async Task<Ticket?> GetTicketByIdAsync(Guid ticketId)
    {
        try
        {
            _logger.LogInformation("Getting ticket with ID: {TicketId}", ticketId);
            return await _context.Tickets.FindAsync(ticketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting ticket with ID: {TicketId}", ticketId);
            throw new RepositoryException($"Failed to retrieve ticket with ID: {ticketId}", ex);
        }
    }

    public async Task<IEnumerable<Ticket>> GetTicketsByEventIdAsync(long eventId)
    {
        try
        {
            _logger.LogInformation("Getting tickets for event ID: {EventId}", eventId);
            return await _context.Tickets
                .Where(t => t.EventId == eventId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting tickets for event ID: {EventId}", eventId);
            throw new RepositoryException($"Failed to retrieve tickets for event ID: {eventId}", ex);
        }
    }

    public async Task<Ticket> CreateTicketAsync(Ticket request)
    {
        if (request == null)
        {
            _logger.LogError("Ticket object is null");
            throw new ArgumentNullException(nameof(request));
        }

        try
        {
            _logger.LogInformation("Creating ticket for event ID: {EventId}", request.EventId);
            _context.Tickets.Add(request);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully created ticket with ID: {TicketId}", request.TicketId);
            return request;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error occurred while creating ticket for event ID: {EventId}", request.EventId);
            throw new RepositoryException("Failed to create ticket due to database error", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating ticket for event ID: {EventId}", request.EventId);
            throw new RepositoryException("Failed to create ticket", ex);
        }
    }

    public async Task<bool> UpdateTicketStatusAsync(Guid ticketId, TicketStatus status)
    {
        try
        {
            _logger.LogInformation("Updating status to {Status} for ticket ID: {TicketId}", status, ticketId);
            
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
            {
                _logger.LogWarning("Ticket with ID: {TicketId} not found for status update", ticketId);
                return false;
            }

            // Check if the status transition is valid
            if (!IsValidStatusTransition(ticket.Status, status))
            {
                _logger.LogWarning("Invalid status transition from {CurrentStatus} to {NewStatus} for ticket ID: {TicketId}", 
                    ticket.Status, status, ticketId);
                throw new InvalidOperationException($"Invalid status transition from {ticket.Status} to {status}");
            }

            ticket.Status = status;
            
            // Update timestamps based on status
            if (status == TicketStatus.ONHOLD)
            {
                ticket.BookingDate = DateTime.UtcNow;
            }
            else if (status == TicketStatus.SOLD)
            {
                ticket.SoldDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully updated status to {Status} for ticket ID: {TicketId}", status, ticketId);
            return true;
        }
        catch (InvalidOperationException)
        {
            // Re-throw the invalid operation exception as it contains a meaningful message
            throw;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency conflict updating status for ticket ID: {TicketId}", ticketId);
            throw new RepositoryException($"Concurrency conflict when updating ticket with ID: {ticketId}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating status for ticket ID: {TicketId}", ticketId);
            throw new RepositoryException($"Failed to update ticket status with ID: {ticketId}", ex);
        }
    }

    public async Task<bool> ReserveTicketsAsync(IEnumerable<Guid> ticketIds, long eventId)
    {
        var ticketList = ticketIds.ToList();
        if (!ticketList.Any())
        {
            _logger.LogWarning("No ticket IDs provided for reservation for event ID: {EventId}", eventId);
            return false;
        }

        _logger.LogInformation("Reserving {Count} tickets for event ID: {EventId}", ticketList.Count, eventId);

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SET TRANSACTION ISOLATION LEVEL READ COMMITTED");

                var updatedCount = await _context.Tickets
                    .Where(t => ticketList.Contains(t.TicketId) && t.Status == TicketStatus.ONHOLD)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(t => t.Status, TicketStatus.SOLD)
                        .SetProperty(t => t.SoldDate, DateTime.UtcNow));

                if (updatedCount != ticketList.Count)
                {
                    _logger.LogWarning("Expected to reserve {ExpectedCount} tickets but only reserved {ActualCount}. " +
                        "Some tickets may not exist, may not be ONHOLD, or were modified concurrently.",
                        ticketList.Count, updatedCount);
                    await transaction.RollbackAsync();
                    return false;
                }

                await transaction.CommitAsync();
                _logger.LogInformation("Successfully reserved {Count} tickets for event ID: {EventId}", updatedCount, eventId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error occurred while reserving tickets for event ID: {EventId}", eventId);
                throw new RepositoryException($"Failed to reserve tickets for event ID: {eventId}", ex);
            }
        });
    }

    public async Task<bool> ReleaseTicketsAsync(IEnumerable<Guid> ticketIds, long eventId)
    {
        var ticketList = ticketIds.ToList();
        if (!ticketList.Any())
        {
            _logger.LogWarning("No ticket IDs provided for release for event ID: {EventId}", eventId);
            return false;
        }

        _logger.LogInformation("Releasing {Count} tickets for event ID: {EventId}", ticketList.Count, eventId);

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SET TRANSACTION ISOLATION LEVEL READ COMMITTED");

                var updatedCount = await _context.Tickets
                    .Where(t => ticketList.Contains(t.TicketId) && t.Status == TicketStatus.ONHOLD)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(t => t.Status, TicketStatus.AVAILABLE)
                        .SetProperty(t => t.BookingDate, (DateTime?)null));

                if (updatedCount != ticketList.Count)
                {
                    _logger.LogWarning("Expected to release {ExpectedCount} tickets but only released {ActualCount}. " +
                        "Some tickets may not exist, may not be ONHOLD, or were modified concurrently.",
                        ticketList.Count, updatedCount);
                    await transaction.RollbackAsync();
                    return false;
                }

                await transaction.CommitAsync();
                _logger.LogInformation("Successfully released {Count} tickets for event ID: {EventId}", updatedCount, eventId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error occurred while releasing tickets for event ID: {EventId}", eventId);
                throw new RepositoryException($"Failed to release tickets for event ID: {eventId}", ex);
            }
        });
    }

    public async Task<bool> DeleteTicketAsync(Guid ticketId)
    {
        try
        {
            _logger.LogInformation("Deleting ticket with ID: {TicketId}", ticketId);
            
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
            {
                _logger.LogWarning("Ticket with ID: {TicketId} not found for deletion", ticketId);
                return false;
            }

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully deleted ticket with ID: {TicketId}", ticketId);
            return true;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error occurred while deleting ticket ID: {TicketId}", ticketId);
            throw new RepositoryException($"Failed to delete ticket with ID: {ticketId} due to database constraints", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting ticket ID: {TicketId}", ticketId);
            throw new RepositoryException($"Failed to delete ticket with ID: {ticketId}", ex);
        }
    }
    
    private static bool IsValidStatusTransition(TicketStatus currentStatus, TicketStatus newStatus)
    {
        // Define valid status transitions
        switch (currentStatus)
        {
            case TicketStatus.PENDING:
                return newStatus == TicketStatus.AVAILABLE || newStatus == TicketStatus.CANCELLED;
                
            case TicketStatus.AVAILABLE:
                return newStatus == TicketStatus.ONHOLD || newStatus == TicketStatus.SOLD || newStatus == TicketStatus.CANCELLED;
                
            case TicketStatus.ONHOLD:
                return newStatus == TicketStatus.AVAILABLE || newStatus == TicketStatus.SOLD || newStatus == TicketStatus.CANCELLED;
                
            case TicketStatus.SOLD:
                return newStatus == TicketStatus.CANCELLED; // Sold tickets can only be cancelled
                
            case TicketStatus.CANCELLED:
                return false; // Cancelled is a terminal state
                
            default:
                return false;
        }
    }
}
