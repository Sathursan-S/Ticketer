using BookingService.Domain.Dtos;
using BookingService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace BookingService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingController> _logger;

    public BookingController(IBookingService bookingService, ILogger<BookingController> logger)
    {
        _bookingService = bookingService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new booking
    /// </summary>
    /// <param name="dto">The booking information</param>
    /// <returns>The created booking</returns>
    [HttpPost]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        try
        {
            var result = await _bookingService.CreateBookingAsync(dto);
            return CreatedAtAction(nameof(GetBookingById), new { bookingId = result.BookingId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create booking");
            return StatusCode(500, "An error occurred while creating the booking.");
        }
    }

    /// <summary>
    /// Gets a booking by its ID
    /// </summary>
    /// <param name="bookingId">The ID of the booking to retrieve</param>
    /// <returns>The booking details</returns>
    [HttpGet("{bookingId}")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBookingById(Guid bookingId)
    {
        try
        {
            var booking = await _bookingService.GetBookingByIdAsync(bookingId);
            if (booking == null)
                return NotFound();
            return Ok(booking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve booking {BookingId}", bookingId);
            return StatusCode(500, "An error occurred while retrieving the booking.");
        }
    }

    /// <summary>
    /// Gets all bookings
    /// </summary>
    /// <returns>A list of all bookings</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllBookings()
    {
        try
        {
            var bookings = await _bookingService.GetAllBookingsAsync();
            return Ok(bookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all bookings");
            return StatusCode(500, "An error occurred while retrieving bookings.");
        }
    }

    /// <summary>
    /// Updates the status of a booking
    /// </summary>
    /// <param name="bookingId">The ID of the booking to update</param>
    /// <param name="status">The new status</param>
    /// <returns>No content if successful</returns>
    [HttpPut("{bookingId}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateBookingStatus(Guid bookingId, [FromBody] string status)
    {
        try
        {
            var updated = await _bookingService.UpdateBookingStatusAsync(bookingId, status);
            if (!updated)
                return NotFound();
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid status value for booking {BookingId}", bookingId);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status for booking {BookingId}", bookingId);
            return StatusCode(500, "An error occurred while updating the booking status.");
        }
    }

    /// <summary>
    /// Gets the status of a booking
    /// </summary>
    /// <param name="bookingId">The ID of the booking</param>
    /// <returns>The booking status</returns>
    [HttpGet("{bookingId}/status")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetBookingStatus(Guid bookingId)
    {
        try
        {
            var status = await _bookingService.GetBookingStatusAsync(bookingId);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get status for booking {BookingId}", bookingId);
            return StatusCode(500, "An error occurred while retrieving the booking status.");
        }
    }

    /// <summary>
    /// Cancels a booking
    /// </summary>
    /// <param name="bookingId">The ID of the booking to cancel</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{bookingId}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelBooking(Guid bookingId)
    {
        try
        {
            var canceled = await _bookingService.CancelBookingAsync(bookingId);
            if (!canceled)
                return NotFound();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel booking {BookingId}", bookingId);
            return StatusCode(500, "An error occurred while canceling the booking.");
        }
    }

    /// <summary>
    /// Marks a booking as successful
    /// </summary>
    /// <param name="bookingId">The ID of the booking to mark as successful</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{bookingId}/success")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> MarkBookingAsSuccessful(Guid bookingId)
    {
        try
        {
            var success = await _bookingService.MarkBookingAsSuccessfulAsync(bookingId);
            if (!success)
                return NotFound();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark booking {BookingId} as successful", bookingId);
            return StatusCode(500, "An error occurred while marking the booking as successful.");
        }
    }

    /// <summary>
    /// Deletes a booking
    /// </summary>
    /// <param name="bookingId">The ID of the booking to delete</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{bookingId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteBooking(Guid bookingId)
    {
        try
        {
            var deleted = await _bookingService.DeleteBookingAsync(bookingId);
            if (!deleted)
                return NotFound();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete booking {BookingId}", bookingId);
            return StatusCode(500, "An error occurred while deleting the booking.");
        }
    }
}
