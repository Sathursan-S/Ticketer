using AutoMapper;
using Microsoft.Extensions.Logging;
using RedLockNet.SERedis;
using SharedLibrary.Contracts.Messages;
using TicketService.DTOs;
using TicketService.Repositoy;
using TicketService.Services;
using ServiceException = TicketService.Application.Exceptions.ServiceException;

namespace TicketService.Application.Services;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<TicketService> _logger;
    private readonly RedLockFactory _redLockFactory;

    public TicketService(
        ITicketRepository ticketRepository, 
        IMapper mapper, 
        ILogger<TicketService> logger,
        RedLockFactory redLockFactory)
    {
        _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _redLockFactory = redLockFactory ?? throw new ArgumentNullException(nameof(redLockFactory));
    }

    public async Task<HoldTicketResponse> HoldTicketsAsync(HoldTickets request)
    {
        try
        {
            _logger.LogInformation(
                "Processing hold request for {NumberOfTickets} tickets for event {EventId} for booking {BookingId}", 
                request.NumberOfTickets, 
                request.EventId, 
                request.BookingId);

            // Get available tickets for the event - do this outside the lock
            var availableTickets = await _ticketRepository.GetTicketsByEventIdAsync(request.EventId);
            var availableList = availableTickets.Where(t => t.Status == TicketStatus.AVAILABLE).ToList();
            
            if (availableList.Count < request.NumberOfTickets)
            {
                _logger.LogWarning(
                    "Not enough available tickets for event {EventId}. Requested: {Requested}, Available: {Available}", 
                    request.EventId, 
                    request.NumberOfTickets, 
                    availableList.Count);
                
                return new HoldTicketResponse
                {
                    EventId = request.EventId,
                    TicketIds = new List<Guid>(),
                    Status = TicketHoldStatus.FAILED,
                    ErrorMessage = $"Not enough available tickets. Requested: {request.NumberOfTickets}, Available: {availableList.Count}"
                };
            }
            
            // Take the requested number of tickets - still outside the lock
            var ticketsToHold = availableList.Take(request.NumberOfTickets).ToList();
            var heldTicketIds = new List<Guid>();
            
            // Create a unique resource key for this event
            var resourceKey = $"event:{request.EventId}:tickets:hold";
            
            // Configure the lock with appropriate timeout and retry settings
            var expiry = TimeSpan.FromSeconds(10);       // Lock expiration time - shortened for efficiency
            var wait = TimeSpan.FromSeconds(5);          // Time to wait to acquire lock
            var retry = TimeSpan.FromMilliseconds(200);  // Time between retries
            
            // Now acquire distributed lock only for the critical section - updating ticket status
            using (var redLock = await _redLockFactory.CreateLockAsync(resourceKey, expiry, wait, retry))
            {
                // If the lock acquisition failed
                if (!redLock.IsAcquired)
                {
                    _logger.LogWarning("Failed to acquire lock for event {EventId}, tickets may be contended", request.EventId);
                    return new HoldTicketResponse
                    {
                        EventId = request.EventId,
                        TicketIds = new List<Guid>(),
                        Status = TicketHoldStatus.FAILED,
                        ErrorMessage = "Could not process request due to high contention, please try again later"
                    };
                }

                _logger.LogInformation("Lock acquired for event {EventId}", request.EventId);
                
                // Inside the lock: verify tickets are still available (they might have been taken since we checked)
                var freshAvailableTickets = await _ticketRepository.GetTicketsByEventIdAsync(request.EventId);
                var stillAvailableTicketIds = freshAvailableTickets
                    .Where(t => t.Status == TicketStatus.AVAILABLE)
                    .Select(t => t.TicketId)
                    .ToHashSet();
                
                // Verify our selected tickets are still available
                var unavailableTickets = ticketsToHold.Where(t => !stillAvailableTicketIds.Contains(t.TicketId)).ToList();
                if (unavailableTickets.Any())
                {
                    _logger.LogWarning(
                        "{UnavailableCount} tickets were taken before lock was acquired for event {EventId}", 
                        unavailableTickets.Count, 
                        request.EventId);
                    
                    return new HoldTicketResponse
                    {
                        EventId = request.EventId,
                        TicketIds = new List<Guid>(),
                        Status = TicketHoldStatus.FAILED,
                        ErrorMessage = "Some tickets were taken before your request could be processed"
                    };
                }
                
                // Update each ticket's status to ONHOLD - this is the critical section
                foreach (var ticket in ticketsToHold)
                {
                    var updated = await _ticketRepository.UpdateTicketStatusAsync(ticket.TicketId, TicketStatus.ONHOLD);
                    
                    if (updated)
                    {
                        heldTicketIds.Add(ticket.TicketId);
                        _logger.LogInformation("Ticket {TicketId} status changed to ONHOLD", ticket.TicketId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to update ticket {TicketId} status to ONHOLD", ticket.TicketId);
                    }
                }
            } // Lock is released here
            
            // Handle partial success scenario outside of the lock
            if (heldTicketIds.Count < request.NumberOfTickets)
            {
                _logger.LogWarning(
                    "Could only hold {HeldCount} of {RequestedCount} tickets for event {EventId}, rolling back", 
                    heldTicketIds.Count, 
                    request.NumberOfTickets, 
                    request.EventId);
                
                // Re-acquire the lock for rolling back
                using (var rollbackLock = await _redLockFactory.CreateLockAsync(resourceKey, expiry, wait, retry))
                {
                    if (rollbackLock.IsAcquired)
                    {
                        // Release any tickets that were held
                        foreach (var ticketId in heldTicketIds)
                        {
                            await _ticketRepository.UpdateTicketStatusAsync(ticketId, TicketStatus.AVAILABLE);
                            _logger.LogInformation("Rolled back hold for ticket {TicketId}", ticketId);
                        }
                    }
                    else
                    {
                        _logger.LogError("Could not acquire lock for rollback, some tickets may remain in ONHOLD state");
                    }
                }
                
                return new HoldTicketResponse
                {
                    EventId = request.EventId,
                    TicketIds = new List<Guid>(),
                    Status = TicketHoldStatus.FAILED,
                    ErrorMessage = "Failed to hold all requested tickets due to a concurrent update"
                };
            }
            
            _logger.LogInformation(
                "Successfully held {Count} tickets for event {EventId} for booking {BookingId}", 
                heldTicketIds.Count, 
                request.EventId, 
                request.BookingId);
            
            return new HoldTicketResponse
            {
                EventId = request.EventId,
                TicketIds = heldTicketIds,
                Status = TicketHoldStatus.SUCCESS
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while holding tickets for event {EventId}", request.EventId);
            return new HoldTicketResponse
            {
                EventId = request.EventId,
                TicketIds = new List<Guid>(),
                Status = TicketHoldStatus.FAILED,
                ErrorMessage = $"An unexpected error occurred: {ex.Message}"
            };
        }
    }

    public async Task<IEnumerable<TicketResponse>> GetAllTicketsAsync()
    {
        try
        {
            _logger.LogInformation("Getting all tickets");
            var tickets = await _ticketRepository.GetTicketsAsync();
            return _mapper.Map<IEnumerable<TicketResponse>>(tickets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all tickets");
            throw new ServiceException("Failed to retrieve tickets", ex);
        }
    }

    public async Task<TicketResponse?> GetTicketByIdAsync(Guid ticketId)
    {
        try
        {
            _logger.LogInformation("Getting ticket with ID: {TicketId}", ticketId);
            var ticket = await _ticketRepository.GetTicketByIdAsync(ticketId);
            
            if (ticket == null)
            {
                _logger.LogWarning("Ticket with ID: {TicketId} not found", ticketId);
                return null;
            }
            
            return _mapper.Map<TicketResponse>(ticket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting ticket with ID: {TicketId}", ticketId);
            throw new ServiceException($"Failed to retrieve ticket with ID: {ticketId}", ex);
        }
    }

    public async Task<IEnumerable<TicketResponse>> GetTicketsByEventIdAsync(long eventId)
    {
        try
        {
            _logger.LogInformation("Getting tickets for event ID: {EventId}", eventId);
            var tickets = await _ticketRepository.GetTicketsByEventIdAsync(eventId);
            return _mapper.Map<IEnumerable<TicketResponse>>(tickets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting tickets for event ID: {EventId}", eventId);
            throw new ServiceException($"Failed to retrieve tickets for event ID: {eventId}", ex);
        }
    }

    public async Task<TicketResponse> CreateTicketAsync(CreateTicketRequest request)
    {
        try
        {
            // Request cannot be null due to nullable reference types
            
            _logger.LogInformation("Creating ticket for event ID: {EventId}", request.EventId);
            
            // Create a new ticket from the request
            var ticket = new Ticket
            {
                EventId = request.EventId,
                Status = TicketStatus.AVAILABLE
            };
            
            var createdTicket = await _ticketRepository.CreateTicketAsync(ticket);
            _logger.LogInformation("Successfully created ticket with ID: {TicketId}", createdTicket.TicketId);
            
            return _mapper.Map<TicketResponse>(createdTicket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating ticket for event ID: {EventId}", request.EventId);
            throw new ServiceException($"Failed to create ticket for event ID: {request.EventId}", ex);
        }
    }

    public async Task<TicketStatusResponse?> UpdateTicketStatusAsync(Guid ticketId, TicketStatus status)
    {
        try
        {
            _logger.LogInformation("Updating status to {Status} for ticket ID: {TicketId}", status, ticketId);
            
            // First, get the ticket to ensure it exists and to get its event ID for the response
            var ticket = await _ticketRepository.GetTicketByIdAsync(ticketId);
            if (ticket == null)
            {
                _logger.LogWarning("Ticket with ID: {TicketId} not found for status update", ticketId);
                return null;
            }
            
            // Try to update the status
            var updated = await _ticketRepository.UpdateTicketStatusAsync(ticketId, status);
            if (!updated)
            {
                _logger.LogWarning("Failed to update status for ticket ID: {TicketId}", ticketId);
                return null;
            }
            
            _logger.LogInformation("Successfully updated status to {Status} for ticket ID: {TicketId}", status, ticketId);
            
            // Return the updated status info
            return new TicketStatusResponse
            {
                TicketId = ticketId,
                EventId = ticket.EventId,
                Status = status
            };
        }
        catch (InvalidOperationException ex)
        {
            // Invalid status transition
            _logger.LogWarning(ex, "Invalid status transition for ticket ID: {TicketId}", ticketId);
            throw new ServiceException($"Invalid status transition for ticket ID: {ticketId}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating status for ticket ID: {TicketId}", ticketId);
            throw new ServiceException($"Failed to update ticket status with ID: {ticketId}", ex);
        }
    }

    public async Task<bool> DeleteTicketAsync(Guid ticketId)
    {
        try
        {
            _logger.LogInformation("Deleting ticket with ID: {TicketId}", ticketId);
            var result = await _ticketRepository.DeleteTicketAsync(ticketId);
            
            if (result)
            {
                _logger.LogInformation("Successfully deleted ticket with ID: {TicketId}", ticketId);
            }
            else
            {
                _logger.LogWarning("Ticket with ID: {TicketId} not found for deletion", ticketId);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting ticket ID: {TicketId}", ticketId);
            throw new ServiceException($"Failed to delete ticket with ID: {ticketId}", ex);
        }
    }

    public async Task<int> CreateBulkTicketsAsync(CreateTicketRequest request)
    {
        try
        {
            // Request cannot be null due to nullable reference types
            
            if (request.Quantity <= 0)
            {
                _logger.LogError("Invalid quantity specified for bulk ticket creation: {Quantity}", request.Quantity);
                throw new ArgumentException("Quantity must be greater than zero", "request");
            }
            
            _logger.LogInformation("Creating {Quantity} tickets for event ID: {EventId}", request.Quantity, request.EventId);
            
            int createdCount = 0;
            
            // Create the specified number of tickets
            for (int i = 0; i < request.Quantity; i++)
            {
                var ticket = new Ticket
                {
                    EventId = request.EventId,
                    Status = TicketStatus.AVAILABLE
                };
                
                await _ticketRepository.CreateTicketAsync(ticket);
                createdCount++;
            }
            
            _logger.LogInformation("Successfully created {Count} tickets for event ID: {EventId}", createdCount, request.EventId);
            return createdCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating bulk tickets for event ID: {EventId}", request.EventId);
            throw new ServiceException($"Failed to create bulk tickets for event ID: {request.EventId}", ex);
        }
    }
}
