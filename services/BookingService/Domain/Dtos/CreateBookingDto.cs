namespace BookingService.Domain.Dtos;

public class CreateBookingDto
{
    public required string CustomerId { get; set; }
    public required int EventId { get; set; }
    public required int NumberOfTickets { get; set; }
}
