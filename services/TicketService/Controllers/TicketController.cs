using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TicketService.Application.Services;
using TicketService.DTOs;
using TicketService.Services;

namespace TicketService.Controllers;

// API Response Constants
public static class ApiResponses
{
    public const string InternalServerError = "Internal server error. Please try again later.";
}

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Consumes(MediaTypeNames.Application.Json)]
public class TicketController : ControllerBase
{
    private readonly ITicketService _ticketService;
    private readonly ILogger<TicketController> _logger;

    public TicketController(ITicketService ticketService, ILogger<TicketController> logger)
    {
        _ticketService = ticketService ?? throw new ArgumentNullException(nameof(ticketService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all tickets
    /// </summary>
    /// <returns>A collection of tickets</returns>
    /// <response code="200">Returns the list of tickets</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<TicketResponse>>> GetTickets()
    {
        try
        {
            _logger.LogInformation("API Request: Getting all tickets");
            var result = await _ticketService.GetAllTicketsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting all tickets");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponses.InternalServerError);
        }
    }

    /// <summary>
    /// Gets a specific ticket by id
    /// </summary>
    /// <param name="id">The ticket id</param>
    /// <returns>The ticket information</returns>
    /// <response code="200">Returns the ticket</response>
    /// <response code="404">If the ticket was not found</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TicketResponse>> GetTicket(Guid id)
    {
        try
        {
            _logger.LogInformation("API Request: Getting ticket with ID: {TicketId}", id);
            var result = await _ticketService.GetTicketByIdAsync(id);
            
            if (result == null)
            {
                return NotFound($"Ticket with ID {id} not found");
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting ticket with ID: {TicketId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponses.InternalServerError);
        }
    }

    /// <summary>
    /// Gets all tickets for a specific event
    /// </summary>
    /// <param name="eventId">The event id</param>
    /// <returns>A collection of tickets for the event</returns>
    /// <response code="200">Returns the list of tickets for the event</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpGet("event/{eventId:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<TicketResponse>>> GetTicketsByEvent(long eventId)
    {
        try
        {
            _logger.LogInformation("API Request: Getting tickets for event ID: {EventId}", eventId);
            var result = await _ticketService.GetTicketsByEventIdAsync(eventId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting tickets for event ID: {EventId}", eventId);
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponses.InternalServerError);
        }
    }

    /// <summary>
    /// Creates a new ticket
    /// </summary>
    /// <param name="request">The ticket creation request</param>
    /// <returns>The created ticket</returns>
    /// <response code="201">Returns the newly created ticket</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<TicketResponse>>> CreateTicket([FromBody] CreateTicketRequest request)
    {
        try
        {
            if (request.Quantity > 1)
            {
                // If quantity > 1, create bulk tickets but return just the first one created
                _logger.LogInformation("Redirecting to bulk creation for {Quantity} tickets", request.Quantity);
                var tickets = await _ticketService.CreateBulkTicketsAsync(request);
                return Ok(tickets);
            }
            
            _logger.LogInformation("API Request: Creating ticket for event ID: {EventId}", request.EventId);
            
            // Validate the request
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var result = await _ticketService.CreateTicketAsync(request);
            
            // Return 201 Created with the location header pointing to the newly created resource
            return CreatedAtAction(
                nameof(GetTicket),
                new { id = result.TicketId },
                result
            );
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while creating ticket");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating ticket for event ID: {EventId}", request.EventId);
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponses.InternalServerError);
        }
    }

    /// <summary>
    /// Creates multiple tickets in bulk
    /// </summary>
    /// <param name="request">The bulk ticket creation request</param>
    /// <returns>The number of tickets created</returns>
    /// <response code="201">Returns the number of tickets created</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPost("bulk")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BulkCreationResponse>> CreateBulkTickets([FromBody] CreateTicketRequest request)
    {
        try
        {
            _logger.LogInformation("API Request: Creating {Quantity} tickets for event ID: {EventId}", request.Quantity, request.EventId);
            
            // Validate the request
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            // Additional validation
            if (request.Quantity <= 0)
            {
                return BadRequest("Quantity must be greater than zero");
            }
            
            var count = await _ticketService.CreateBulkTicketsAsync(request);
            
            var response = new BulkCreationResponse
            {
                EventId = request.EventId,
                TicketsCreated = count,
                Message = $"Successfully created {count} tickets for event {request.EventId}"
            };
            
            // Return 201 Created with a custom response
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument while creating bulk tickets");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating bulk tickets for event ID: {EventId}", request.EventId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = ApiResponses.InternalServerError, Exception = ex.Message });
        }
    }

    /// <summary>
    /// Updates the status of a ticket
    /// </summary>
    /// <param name="id">The ticket id</param>
    /// <param name="request">The status update request</param>
    /// <returns>The updated ticket status</returns>
    /// <response code="200">Returns the updated ticket status</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="404">If the ticket was not found</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TicketStatusResponse>> UpdateTicketStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            _logger.LogInformation("API Request: Updating status to {Status} for ticket ID: {TicketId}", request.Status, id);
            
            // Validate the request
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var result = await _ticketService.UpdateTicketStatusAsync(id, request.Status);
            
            if (result == null)
            {
                return NotFound($"Ticket with ID {id} not found");
            }
            
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid status transition for ticket ID: {TicketId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating status for ticket ID: {TicketId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponses.InternalServerError);
        }
    }

    /// <summary>
    /// Deletes a specific ticket
    /// </summary>
    /// <param name="id">The ticket id</param>
    /// <returns>No content</returns>
    /// <response code="204">If the ticket was successfully deleted</response>
    /// <response code="404">If the ticket was not found</response>
    /// <response code="500">If there was an internal server error</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteTicket(Guid id)
    {
        try
        {
            _logger.LogInformation("API Request: Deleting ticket with ID: {TicketId}", id);
            
            var result = await _ticketService.DeleteTicketAsync(id);
            
            if (!result)
            {
                return NotFound($"Ticket with ID {id} not found");
            }
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting ticket ID: {TicketId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponses.InternalServerError);
        }
    }
}