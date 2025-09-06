using Microsoft.AspNetCore.Mvc;
using PaymentService.Domain;
using PaymentService.Application.Services;

namespace PaymentService.Controllers;

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
    [HttpPost("process")]
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

    /// <summary>
    /// Processes a refund
    /// </summary>
    /// <param name="dto">The refund information</param>
    /// <returns>The refund result</returns>
    [HttpPost("refund")]
    [ProducesResponseType(typeof(RefundResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefundPayment([FromBody] RefundRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
            
        try
        {
            var result = await _paymentService.RefundPaymentAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process refund");
            return StatusCode(500, "An error occurred while processing the refund.");
        }
    }

    /// <summary>
    /// Handles webhook notifications from payment gateways
    /// </summary>
    /// <param name="gatewayName">The name of the payment gateway</param>
    /// <returns>Success or failure status</returns>
    [HttpPost("webhook/{gatewayName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HandleWebhook(string gatewayName)
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync();
            
            var signature = Request.Headers["Stripe-Signature"].FirstOrDefault() ?? "";
            
            var result = await _paymentService.ProcessWebhookAsync(payload, signature, gatewayName);
            
            if (result)
                return Ok();
            else
                return BadRequest("Webhook processing failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process webhook for gateway: {GatewayName}", gatewayName);
            return StatusCode(500, "An error occurred while processing the webhook.");
        }
    }
}