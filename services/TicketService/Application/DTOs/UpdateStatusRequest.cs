using System.Text.Json.Serialization;

namespace TicketService.DTOs;

public class UpdateStatusRequest
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TicketStatus Status { get; set; }
}
