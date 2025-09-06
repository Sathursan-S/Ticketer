using System;
using System.Text.Json.Serialization;

namespace TicketService.Contracts.Messages;

public record CreateEventTicket(
    [property: JsonPropertyName("eventId")] long EventId,
    [property: JsonPropertyName("numberOfTickets")] int NumberOfTickets
);
