using System.ComponentModel.DataAnnotations;

namespace BookingService.Services;

/// <summary>
/// Service for email validation and notification
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Validates email format
    /// </summary>
    bool IsValidEmail(string email);
    
    /// <summary>
    /// Sends booking confirmation email with QR code
    /// </summary>
    Task SendBookingConfirmationAsync(Guid bookingId, string email, string eventName, int numberOfTickets, decimal totalAmount);
    
    /// <summary>
    /// Sends booking failure notification
    /// </summary>
    Task SendBookingFailureAsync(Guid bookingId, string email, string reason);
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var emailAttribute = new EmailAddressAttribute();
            return emailAttribute.IsValid(email);
        }
        catch
        {
            return false;
        }
    }

    public async Task SendBookingConfirmationAsync(Guid bookingId, string email, string eventName, int numberOfTickets, decimal totalAmount)
    {
        try
        {
            // TODO: Implement actual email sending with QR code
            // For now, just log the confirmation
            _logger.LogInformation(
                "Booking confirmation email prepared for {Email}: BookingId={BookingId}, Event={EventName}, Tickets={NumberOfTickets}, Amount=${TotalAmount}",
                email, bookingId, eventName, numberOfTickets, totalAmount);

            // Simulate QR code generation
            var qrCodeData = GenerateQRCodeData(bookingId, email, eventName, numberOfTickets);
            _logger.LogInformation("QR Code generated: {QRCodeData}", qrCodeData);

            // TODO: Send actual email with:
            // - Booking confirmation details
            // - QR code for ticket validation
            // - Event information
            // - Payment receipt

            await Task.Delay(100); // Simulate async operation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send booking confirmation email to {Email} for booking {BookingId}", email, bookingId);
            throw;
        }
    }

    public async Task SendBookingFailureAsync(Guid bookingId, string email, string reason)
    {
        try
        {
            // TODO: Implement actual email sending
            _logger.LogInformation(
                "Booking failure email prepared for {Email}: BookingId={BookingId}, Reason={Reason}",
                email, bookingId, reason);

            // TODO: Send actual email with:
            // - Booking failure notification
            // - Reason for failure
            // - Next steps for customer

            await Task.Delay(100); // Simulate async operation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send booking failure email to {Email} for booking {BookingId}", email, bookingId);
            throw;
        }
    }

    private string GenerateQRCodeData(Guid bookingId, string email, string eventName, int numberOfTickets)
    {
        // Generate QR code data containing booking information
        // This would typically be encoded as JSON or a custom format
        return $"TICKET|{bookingId}|{email}|{eventName}|{numberOfTickets}|{DateTime.UtcNow:yyyy-MM-dd}";
    }
}