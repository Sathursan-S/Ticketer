using BookingService.Domain.Dtos;
using BookingService.Domain.Entities;

namespace BookingService.Services;

public interface IBookingService
{
    // Create a new booking
    Task<BookingDto> CreateBookingAsync(CreateBookingDto createBookingDto);

    // Cancel an existing booking
    Task<bool> CancelBookingAsync(Guid bookingId);

    // Mark a booking as successful
    Task<bool> MarkBookingAsSuccessfulAsync(Guid bookingId);

    // Get the status of a booking
    Task<string> GetBookingStatusAsync(Guid bookingId);

    // Update the status of a booking
    Task<bool> UpdateBookingStatusAsync(Guid bookingId, string status);

    // Retrieve a booking by ID
    Task<BookingDto?> GetBookingByIdAsync(Guid bookingId);

    // Retrieve all bookings
    Task<IEnumerable<BookingDto>> GetAllBookingsAsync();

    // Delete a booking
    Task<bool> DeleteBookingAsync(Guid bookingId);
}
