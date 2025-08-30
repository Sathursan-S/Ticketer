using Microsoft.AspNetCore.Mvc;

namespace BookingService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Processes a payment
    /// </summary>
    /// <param name="dto">The payment information</param>
    /// <returns>The payment result</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        try
        {
            var result = await _paymentService.ProcessPaymentAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process payment");
            return StatusCode(500, "An error occurred while processing the payment.");
        }
    }
}