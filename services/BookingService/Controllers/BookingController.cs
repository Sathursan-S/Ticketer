using BookingService.Domain.Dtos;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Contracts.Events;
using BookingService.Infrastructure.Messaging;
using BookingService.Contracts.Public;


namespace BookingService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BookingController : ControllerBase
{
    private readonly ILogger<BookingController> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IPaymentPublisher _paymentPublisher;
    public BookingController(ILogger<BookingController> logger, IPublishEndpoint publishEndpoint, IPaymentPublisher paymentPublisher)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        _paymentPublisher = paymentPublisher ?? throw new ArgumentNullException(nameof(paymentPublisher));
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
    
    [HttpPost("{bookingId:guid}/payment/success")]
    public async Task<IActionResult> PaymentSuccess(Guid bookingId, [FromBody] PaymentSuccessRequest body)
    {
        var msg = new PaymentSucceeded(
            BookingId: bookingId,
            CustomerId: body.CustomerId,
            PaymentIntentId: body.PaymentIntentId ?? Guid.NewGuid().ToString(),
            Amount: body.Amount,
            Currency: body.Currency ?? "USD",
            PaidAtUtc: DateTime.UtcNow);

        await _paymentPublisher.PublishSuccessAsync(msg);
        _logger.LogInformation("Published payment.succeeded for {BookingId}", bookingId);
        return Accepted(new { published = "payment.succeeded", bookingId });
    }

    [HttpPost("{bookingId:guid}/payment/fail")]
    public async Task<IActionResult> PaymentFail(Guid bookingId, [FromBody] PaymentFailRequest body)
    {
        var msg = new PaymentFailed(
            BookingId: bookingId,
            CustomerId: body.CustomerId,
            Reason: body.Reason ?? "Payment gateway declined",
            FailedAtUtc: DateTime.UtcNow);

        await _paymentPublisher.PublishFailureAsync(msg);
        _logger.LogInformation("Published payment.failed for {BookingId}", bookingId);
        return Accepted(new { published = "payment.failed", bookingId });
    }
}

public record PaymentSuccessRequest(Guid CustomerId, decimal Amount, string? Currency, string? PaymentIntentId);
public record PaymentFailRequest(Guid CustomerId, string? Reason);