using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketService;

[Table("Tickets")]
public class Ticket
{
    [Key]
    public Guid TicketId { get; set; } = Guid.NewGuid();
    [Required]
    public long EventId { get; set; }
    [Required]
    public TicketStatus Status { get; set; } = TicketStatus.AVAILABLE;
    public DateTime? BookingDate { get; set; }
    public DateTime? SoldDate { get; set; }
}