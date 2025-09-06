using Microsoft.Extensions.Logging;
using Moq;
using PaymentService.Application.Gateways;
using PaymentService.Application.Services;
using PaymentService.Domain;
using Xunit;

namespace PaymentService.Tests;

public class PaymentServiceTests
{
    private readonly Mock<ILogger<Application.Services.PaymentService>> _mockLogger;
    private readonly Mock<IPaymentGateway> _mockGateway;
    private readonly IPaymentService _paymentService;

    public PaymentServiceTests()
    {
        _mockLogger = new Mock<ILogger<Application.Services.PaymentService>>();
        _mockGateway = new Mock<IPaymentGateway>();
        _mockGateway.Setup(x => x.GatewayName).Returns("Stripe");
        
        _paymentService = new Application.Services.PaymentService(
            new[] { _mockGateway.Object }, 
            _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithValidDto_ShouldReturnSuccessResult()
    {
        // Arrange
        var dto = new ProcessPaymentDto
        {
            BookingId = Guid.NewGuid(),
            CustomerId = "cust_123",
            Amount = 100.00m,
            PaymentMethod = "pm_123"
        };

        var expectedResult = new PaymentResultDto
        {
            PaymentIntentId = "pi_123",
            Status = "succeeded",
            Amount = dto.Amount,
            BookingId = dto.BookingId,
            PaymentMethod = dto.PaymentMethod,
            CustomerId = dto.CustomerId,
            IsSuccess = true,
            PayedAt = DateTime.UtcNow
        };

        _mockGateway.Setup(x => x.ProcessPaymentAsync(dto))
                   .ReturnsAsync(expectedResult);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("pi_123", result.PaymentIntentId);
        Assert.Equal("succeeded", result.Status);
        Assert.Equal(dto.Amount, result.Amount);
        Assert.Equal(dto.BookingId, result.BookingId);
    }

    [Fact]
    public async Task RefundPaymentAsync_WithValidDto_ShouldReturnSuccessResult()
    {
        // Arrange
        var dto = new RefundRequestDto
        {
            PaymentIntentId = "pi_123",
            Amount = 50.00m,
            Reason = "requested_by_customer",
            BookingId = Guid.NewGuid()
        };

        var expectedResult = new RefundResultDto
        {
            RefundId = "re_123",
            PaymentIntentId = dto.PaymentIntentId,
            IsSuccess = true,
            Amount = dto.Amount,
            Status = "succeeded",
            BookingId = dto.BookingId,
            RefundedAt = DateTime.UtcNow
        };

        _mockGateway.Setup(x => x.RefundPaymentAsync(dto))
                   .ReturnsAsync(expectedResult);

        // Act
        var result = await _paymentService.RefundPaymentAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("re_123", result.RefundId);
        Assert.Equal("succeeded", result.Status);
        Assert.Equal(dto.Amount, result.Amount);
    }

    [Fact]
    public async Task ProcessWebhookAsync_WithValidPayload_ShouldReturnTrue()
    {
        // Arrange
        var payload = "webhook_payload";
        var signature = "webhook_signature";
        var gatewayName = "Stripe";

        _mockGateway.Setup(x => x.ProcessWebhookAsync(payload, signature))
                   .ReturnsAsync(true);

        // Act
        var result = await _paymentService.ProcessWebhookAsync(payload, signature, gatewayName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithNoGateway_ShouldReturnFailureResult()
    {
        // Arrange
        var paymentServiceWithoutGateway = new Application.Services.PaymentService(
            Enumerable.Empty<IPaymentGateway>(),
            _mockLogger.Object);

        var dto = new ProcessPaymentDto
        {
            BookingId = Guid.NewGuid(),
            CustomerId = "cust_123",
            Amount = 100.00m,
            PaymentMethod = "pm_123"
        };

        // Act
        var result = await paymentServiceWithoutGateway.ProcessPaymentAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Equal("No payment gateway available", result.ErrorMessage);
    }
}