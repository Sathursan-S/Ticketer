using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using TicketService.DTOs;
using TicketService.Repositoy;

namespace TicketService.Services;

public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<TicketService> _logger;

    public TicketService(ITicketRepository ticketRepository, IMapper mapper, ILogger<TicketService> logger)
    {
        _ticketRepository = ticketRepository ?? throw new ArgumentNullException(nameof(ticketRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            if (request == null)
            {
                _logger.LogError("CreateTicketRequest is null");
                throw new ArgumentNullException(nameof(request));
            }
            
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
            _logger.LogError(ex, "Error occurred while creating ticket for event ID: {EventId}", request?.EventId);
            throw new ServiceException($"Failed to create ticket for event ID: {request?.EventId}", ex);
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
            if (request == null)
            {
                _logger.LogError("CreateTicketRequest is null for bulk creation");
                throw new ArgumentNullException(nameof(request));
            }
            
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
            _logger.LogError(ex, "Error occurred while creating bulk tickets for event ID: {EventId}", request?.EventId);
            throw new ServiceException($"Failed to create bulk tickets for event ID: {request?.EventId}", ex);
        }
    }
}
