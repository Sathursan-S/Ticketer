using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace BookingService.Domain.Entities;

public class Booking
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid BookingId { get; set; } = Guid.NewGuid();
    
    [Required]
    public string UserId { get; set; }
    
    [Required]
    public int EventId { get; set; }
    
    public List<Guid> TicketIds { get; set; } = new List<Guid>();
    
    [BsonRepresentation(BsonType.String)]
    public BookingStatus Status { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}
