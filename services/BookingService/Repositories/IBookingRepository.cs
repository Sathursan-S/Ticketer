using BookingService.Domain.Entities;

namespace BookingService.Repositories;

public interface IBookingRepository
{
    // Create a new booking
    Task<Booking> CreateBookingAsync(Booking booking);

    // Cancel an existing booking
    Task<bool> CancelBookingAsync(Guid bookingId);

    // Mark a booking as successful
    Task<bool> MarkBookingAsSuccessfulAsync(Guid bookingId);

    // Get the status of a booking
    Task<BookingStatus> GetBookingStatusAsync(Guid bookingId);

    // Update the status of a booking
    Task<bool> UpdateBookingStatusAsync(Guid bookingId, BookingStatus status);

    // Retrieve a booking by ID
    Task<Booking?> GetBookingByIdAsync(Guid bookingId);

    // Retrieve all bookings
    Task<IEnumerable<Booking>> GetAllBookingsAsync();

    // Delete a booking
    Task<bool> DeleteBookingAsync(Guid bookingId);
}