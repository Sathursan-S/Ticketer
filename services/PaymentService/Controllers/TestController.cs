using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PaymentService.Application.Gateways;
using PaymentService.Domain;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;

    public TestController(ILogger<TestController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Test endpoint to verify payment gateway integration
    /// </summary>
    [HttpGet("gateway-info")]
    public IActionResult GetGatewayInfo([FromServices] IEnumerable<IPaymentGateway> gateways)
    {
        var gatewayInfo = gateways.Select(g => new 
        {
            Name = g.GatewayName,
            Type = g.GetType().Name
        }).ToList();

        return Ok(new 
        {
            Message = "Payment Service is running with gateway pattern",
            Gateways = gatewayInfo,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Test endpoint to verify Stripe configuration
    /// </summary>
    [HttpGet("stripe-config")]
    public IActionResult GetStripeConfig([FromServices] IOptions<StripeSettings> stripeOptions)
    {
        var settings = stripeOptions.Value;
        return Ok(new 
        {
            HasSecretKey = !string.IsNullOrEmpty(settings.SecretKey),
            HasPublishableKey = !string.IsNullOrEmpty(settings.PublishableKey),
            HasWebhookSecret = !string.IsNullOrEmpty(settings.WebhookSecret),
            SecretKeyMasked = settings.SecretKey?.Substring(0, Math.Min(7, settings.SecretKey.Length)) + "...",
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Simulate payment processing without external dependencies
    /// </summary>
    [HttpPost("simulate-payment")]
    public IActionResult SimulatePayment([FromBody] ProcessPaymentDto dto)
    {
        _logger.LogInformation("Simulating payment processing for BookingId: {BookingId}", dto.BookingId);

        return Ok(new PaymentResultDto
        {
            PaymentIntentId = $"pi_simulated_{Guid.NewGuid().ToString()[..8]}",
            Status = "succeeded",
            Amount = dto.Amount,
            BookingId = dto.BookingId,
            PaymentMethod = dto.PaymentMethod,
            CustomerId = dto.CustomerId,
            IsSuccess = true,
            PayedAt = DateTime.UtcNow
        });
    }
}