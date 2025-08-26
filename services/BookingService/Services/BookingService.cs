using AutoMapper;
using BookingService.Domain.Dtos;
using BookingService.Domain.Entities;
using BookingService.Repositories;
using Microsoft.Extensions.Logging;

namespace BookingService.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<BookingService> _logger;

    public BookingService(IBookingRepository bookingRepository, IMapper mapper, ILogger<BookingService> logger)
    {
        _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BookingDto> CreateBookingAsync(CreateBookingDto createBookingDto)
    {
        try
        {
            _logger.LogInformation("Creating a new booking.");
            var booking = _mapper.Map<Booking>(createBookingDto);
            var createdBooking = await _bookingRepository.CreateBookingAsync(booking);
            _logger.LogInformation("Booking created successfully with ID {BookingId}.", createdBooking.BookingId);
            return _mapper.Map<BookingDto>(createdBooking);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating a booking.");
            throw new InvalidOperationException("An error occurred while creating a booking.", ex);
        }
    }

    public async Task<bool> CancelBookingAsync(Guid bookingId)
    {
        try
        {
            _logger.LogInformation("Canceling booking with ID {BookingId}.", bookingId);
            var result = await _bookingRepository.CancelBookingAsync(bookingId);
            if (result)
            {
                _logger.LogInformation("Booking with ID {BookingId} canceled successfully.", bookingId);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while canceling the booking with ID {BookingId}.", bookingId);
            throw new InvalidOperationException($"An error occurred while canceling the booking with ID {bookingId}.", ex);
        }
    }

    public async Task<bool> MarkBookingAsSuccessfulAsync(Guid bookingId)
    {
        try
        {
            _logger.LogInformation("Marking booking with ID {BookingId} as successful.", bookingId);
            var result = await _bookingRepository.MarkBookingAsSuccessfulAsync(bookingId);
            if (result)
            {
                _logger.LogInformation("Booking with ID {BookingId} marked as successful.", bookingId);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while marking the booking with ID {BookingId} as successful.", bookingId);
            throw new InvalidOperationException($"An error occurred while marking the booking with ID {bookingId} as successful.", ex);
        }
    }

    public async Task<string> GetBookingStatusAsync(Guid bookingId)
    {
        try
        {
            _logger.LogInformation("Retrieving status for booking with ID {BookingId}.", bookingId);
            var status = await _bookingRepository.GetBookingStatusAsync(bookingId);
            _logger.LogInformation("Status for booking with ID {BookingId} retrieved successfully: {Status}.", bookingId, status);
            return status.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving the status for booking with ID {BookingId}.", bookingId);
            throw new InvalidOperationException($"An error occurred while retrieving the status for booking with ID {bookingId}.", ex);
        }
    }

    public async Task<bool> UpdateBookingStatusAsync(Guid bookingId, string status)
    {
        try
        {
            _logger.LogInformation("Updating status for booking with ID {BookingId} to {Status}.", bookingId, status);
            if (!Enum.TryParse(status, out BookingStatus bookingStatus))
            {
                throw new ArgumentException("Invalid booking status.", nameof(status));
            }

            var result = await _bookingRepository.UpdateBookingStatusAsync(bookingId, bookingStatus);
            if (result)
            {
                _logger.LogInformation("Status for booking with ID {BookingId} updated successfully to {Status}.", bookingId, status);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating the status for booking with ID {BookingId}.", bookingId);
            throw new InvalidOperationException($"An error occurred while updating the status for booking with ID {bookingId}.", ex);
        }
    }

    public async Task<BookingDto?> GetBookingByIdAsync(Guid bookingId)
    {
        try
        {
            _logger.LogInformation("Retrieving booking with ID {BookingId}.", bookingId);
            var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
            if (booking != null)
            {
                _logger.LogInformation("Booking with ID {BookingId} retrieved successfully.", bookingId);
            }
            return booking != null ? _mapper.Map<BookingDto>(booking) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving the booking with ID {BookingId}.", bookingId);
            throw new InvalidOperationException($"An error occurred while retrieving the booking with ID {bookingId}.", ex);
        }
    }

    public async Task<IEnumerable<BookingDto>> GetAllBookingsAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving all bookings.");
            var bookings = await _bookingRepository.GetAllBookingsAsync();
            _logger.LogInformation("All bookings retrieved successfully.");
            return _mapper.Map<IEnumerable<BookingDto>>(bookings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving all bookings.");
            throw new InvalidOperationException("An error occurred while retrieving all bookings.", ex);
        }
    }

    public async Task<bool> DeleteBookingAsync(Guid bookingId)
    {
        try
        {
            _logger.LogInformation("Deleting booking with ID {BookingId}.", bookingId);
            var result = await _bookingRepository.DeleteBookingAsync(bookingId);
            if (result)
            {
                _logger.LogInformation("Booking with ID {BookingId} deleted successfully.", bookingId);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting the booking with ID {BookingId}.", bookingId);
            throw new InvalidOperationException($"An error occurred while deleting the booking with ID {bookingId}.", ex);
        }
    }
}
