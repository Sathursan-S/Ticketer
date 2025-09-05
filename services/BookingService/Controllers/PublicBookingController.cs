using BookingService.Domain.Dtos;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Contracts.Events;
using System.ComponentModel.DataAnnotations;

namespace BookingService.Controllers
{
    /// <summary>
    /// Public booking endpoints for customers (no authentication required)
    /// </summary>
    [ApiController]
    [Route("api/public/booking")]
    [Produces("application/json")]
    public class PublicBookingController : ControllerBase
    {
        private readonly ILogger<PublicBookingController> _logger;
        private readonly IPublishEndpoint _publishEndpoint;

        public PublicBookingController(ILogger<PublicBookingController> logger, IPublishEndpoint publishEndpoint)
        {
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }

        /// <summary>
        /// Creates a new booking for the public (no authentication required)
        /// </summary>
        /// <param name="createBookingDto">The booking details with email validation</param>
        /// <returns>The ID of the created booking</returns>
        /// <response code="201">Returns the newly created booking ID</response>
        /// <response code="400">If the request is invalid or email format is wrong</response>
        /// <response code="500">If there was an internal server error</response>
        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateBooking([FromBody] CreatePublicBookingDto createBookingDto)
        {
            try
            {
                // Validate model
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var bookingId = Guid.NewGuid();
                // Use email as CustomerId for public bookings
                var customerId = createBookingDto.Email;

                await _publishEndpoint.Publish<BookingCreatedEvent>(new
                {
                    BookingId = bookingId,
                    CustomerId = customerId,  // Use email as customer ID
                    EventId = createBookingDto.EventId,
                    NumberOfTickets = createBookingDto.NumberOfTickets,
                    CreatedAt = DateTime.UtcNow
                });

                _logger.LogInformation("Published BookingCreatedEvent for BookingId: {BookingId}, Email: {Email}", 
                    bookingId, createBookingDto.Email);

                return CreatedAtAction(nameof(CreateBooking), new { id = bookingId }, new { 
                    BookingId = bookingId,
                    Message = "Booking created successfully. You will receive a confirmation email shortly.",
                    Email = createBookingDto.Email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating public booking for email: {Email}", createBookingDto.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    "An error occurred while creating the booking. Please try again.");
            }
        }

        /// <summary>
        /// Gets booking status by booking ID (public endpoint for email confirmations)
        /// </summary>
        /// <param name="bookingId">The booking ID</param>
        /// <returns>Booking status information</returns>
        /// <response code="200">Returns the booking status</response>
        /// <response code="404">If the booking was not found</response>
        [HttpGet("{bookingId:guid}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBookingStatus(Guid bookingId)
        {
            try
            {
                // TODO: Implement booking status retrieval
                // For now, return a placeholder response
                return Ok(new 
                { 
                    BookingId = bookingId,
                    Status = "Processing",
                    Message = "Booking is being processed. Check your email for updates."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving booking status for BookingId: {BookingId}", bookingId);
                return NotFound("Booking not found.");
            }
        }
    }
}