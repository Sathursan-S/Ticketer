using BookingService.Domain.Dtos;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Contracts.Events;

namespace BookingService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BookingController : ControllerBase
{
    private readonly ILogger<BookingController> _logger;
    private readonly IPublishEndpoint _publishEndpoint;

    public BookingController(ILogger<BookingController> logger, IPublishEndpoint publishEndpoint)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
    }

/// <summary>
/// Creates a new booking
/// </summary>
/// <param name="createBookingDto">The details of the booking to create</param>
/// <returns>The ID of the created booking</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto createBookingDto)
    {
        // if (!ModelState.IsValid)
        // {
        //     return BadRequest(ModelState);
        // }

        try
        {
            var bookingId = Guid.NewGuid();

            await _publishEndpoint.Publish<BookingCreatedEvent>(new
            {
                BookingId = bookingId,
                CustomerId = createBookingDto.CustomerId,
                EventId = createBookingDto.EventId,
                NumberOfTickets = createBookingDto.NumberOfTickets,
                CreatedAt = DateTime.UtcNow
            });

            _logger.LogInformation("Published BookingCreatedEvent for BookingId: {BookingId}", bookingId);

            return CreatedAtAction(nameof(CreateBooking), new { id = bookingId }, bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the booking.");
        }
    }
}