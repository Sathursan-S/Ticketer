using System.ComponentModel.DataAnnotations;

namespace BookingService.Domain.Dtos;

/// <summary>
/// DTO for creating a public booking with email validation
/// </summary>
public class CreatePublicBookingDto
{
    /// <summary>
    /// Valid email address for booking confirmation
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    public required string Email { get; set; }

    /// <summary>
    /// Event ID to book tickets for
    /// </summary>
    [Required(ErrorMessage = "Event ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Event ID must be a positive number")]
    public required int EventId { get; set; }

    /// <summary>
    /// Number of tickets to book
    /// </summary>
    [Required(ErrorMessage = "Number of tickets is required")]
    [Range(1, 10, ErrorMessage = "Number of tickets must be between 1 and 10")]
    public required int NumberOfTickets { get; set; }
}