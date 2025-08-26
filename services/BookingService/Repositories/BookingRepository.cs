using BookingService.Domain.Entities;
using BookingService.Domain.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookingService.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly IMongoCollection<Booking> _bookings;
    private readonly ILogger<BookingRepository> _logger;

    public BookingRepository(IOptions<MongoDbSettings> settings, ILogger<BookingRepository> logger)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _bookings = database.GetCollection<Booking>(settings.Value.BookingsCollectionName);
        _logger = logger;
    }

    public async Task<Booking> CreateBookingAsync(Booking booking)
    {
        try
        {
            _logger.LogInformation("Adding new booking to database.");
            booking.CreatedAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;
            await _bookings.InsertOneAsync(booking);
            _logger.LogInformation("Booking added with ID {BookingId}.", booking.BookingId);
            return booking;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding booking to database.");
            throw new InvalidOperationException("An error occurred while creating a booking.", ex);
        }
    }

    public async Task<bool> CancelBookingAsync(Guid bookingId)
    {
        try
        {
            _logger.LogInformation("Canceling booking with ID {BookingId}.", bookingId);
            var filter = Builders<Booking>.Filter.Eq(b => b.BookingId, bookingId);
            var update = Builders<Booking>.Update
                .Set(b => b.Status, BookingStatus.CANCELED)
                .Set(b => b.UpdatedAt, DateTime.UtcNow);
                
            var result = await _bookings.UpdateOneAsync(filter, update);
            var success = result.ModifiedCount > 0;
            
            if (success)
            {
                _logger.LogInformation("Booking with ID {BookingId} canceled.", bookingId);
            }
            else
            {
                _logger.LogWarning("No booking found with ID {BookingId} to cancel.", bookingId);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling booking with ID {BookingId}.", bookingId);
            throw new InvalidOperationException($"An error occurred while canceling the booking with ID {bookingId}.", ex);
        }
    }

    public async Task<bool> MarkBookingAsSuccessfulAsync(Guid bookingId)
    {
        try
        {
            _logger.LogInformation("Marking booking with ID {BookingId} as successful.", bookingId);
            var filter = Builders<Booking>.Filter.Eq(b => b.BookingId, bookingId);
            var update = Builders<Booking>.Update
                .Set(b => b.Status, BookingStatus.COMPLETED)
                .Set(b => b.UpdatedAt, DateTime.UtcNow);
                
            var result = await _bookings.UpdateOneAsync(filter, update);
            var success = result.ModifiedCount > 0;
            
            if (success)
            {
                _logger.LogInformation("Booking with ID {BookingId} marked as successful.", bookingId);
            }
            else
            {
                _logger.LogWarning("No booking found with ID {BookingId} to mark as successful.", bookingId);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking booking with ID {BookingId} as successful.", bookingId);
            throw new InvalidOperationException($"An error occurred while marking the booking with ID {bookingId} as successful.", ex);
        }
    }

    public async Task<BookingStatus> GetBookingStatusAsync(Guid bookingId)
    {
        try
        {
            _logger.LogInformation("Getting status for booking with ID {BookingId}.", bookingId);
            var filter = Builders<Booking>.Filter.Eq(b => b.BookingId, bookingId);
            var booking = await _bookings.Find(filter).FirstOrDefaultAsync();
            
            if (booking == null) throw new KeyNotFoundException("Booking not found.");
            
            _logger.LogInformation("Status for booking with ID {BookingId} is {Status}.", bookingId, booking.Status);
            return booking.Status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status for booking with ID {BookingId}.", bookingId);
            throw new InvalidOperationException($"An error occurred while getting the status for booking with ID {bookingId}.", ex);
        }
    }

    public async Task<bool> UpdateBookingStatusAsync(Guid bookingId, BookingStatus status)
    {
        try
        {
            _logger.LogInformation("Updating status for booking with ID {BookingId} to {Status}.", bookingId, status);
            var filter = Builders<Booking>.Filter.Eq(b => b.BookingId, bookingId);
            var update = Builders<Booking>.Update
                .Set(b => b.Status, status)
                .Set(b => b.UpdatedAt, DateTime.UtcNow);
                
            var result = await _bookings.UpdateOneAsync(filter, update);
            var success = result.ModifiedCount > 0;
            
            if (success)
            {
                _logger.LogInformation("Status for booking with ID {BookingId} updated to {Status}.", bookingId, status);
            }
            else
            {
                _logger.LogWarning("No booking found with ID {BookingId} to update status.", bookingId);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for booking with ID {BookingId}.", bookingId);
            throw new InvalidOperationException($"An error occurred while updating the status for booking with ID {bookingId}.", ex);
        }
    }

    public async Task<Booking?> GetBookingByIdAsync(Guid bookingId)
    {
        try
        {
            _logger.LogInformation("Retrieving booking with ID {BookingId}.", bookingId);
            var filter = Builders<Booking>.Filter.Eq(b => b.BookingId, bookingId);
            var booking = await _bookings.Find(filter).FirstOrDefaultAsync();
            
            if (booking != null)
            {
                _logger.LogInformation("Booking with ID {BookingId} retrieved.", bookingId);
            }
            else
            {
                _logger.LogWarning("No booking found with ID {BookingId}.", bookingId);
            }
            
            return booking;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving booking with ID {BookingId}.", bookingId);
            throw new InvalidOperationException($"An error occurred while retrieving the booking with ID {bookingId}.", ex);
        }
    }

    public async Task<IEnumerable<Booking>> GetAllBookingsAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving all bookings.");
            var bookings = await _bookings.Find(Builders<Booking>.Filter.Empty).ToListAsync();
            _logger.LogInformation("All bookings retrieved. Count: {Count}", bookings.Count);
            return bookings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all bookings.");
            throw new InvalidOperationException("An error occurred while retrieving all bookings.", ex);
        }
    }

    public async Task<bool> DeleteBookingAsync(Guid bookingId)
    {
        try
        {
            _logger.LogInformation("Deleting booking with ID {BookingId}.", bookingId);
            var filter = Builders<Booking>.Filter.Eq(b => b.BookingId, bookingId);
            var result = await _bookings.DeleteOneAsync(filter);
            var success = result.DeletedCount > 0;
            
            if (success)
            {
                _logger.LogInformation("Booking with ID {BookingId} deleted.", bookingId);
            }
            else
            {
                _logger.LogWarning("No booking found with ID {BookingId} to delete.", bookingId);
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting booking with ID {BookingId}.", bookingId);
            throw new InvalidOperationException($"An error occurred while deleting the booking with ID {bookingId}.", ex);
        }
    }
}