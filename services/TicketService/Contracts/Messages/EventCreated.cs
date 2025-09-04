using System;
using System.Text.Json.Serialization;

namespace TicketService.Contracts.Messages;

public record EventCreated(
    [property: JsonPropertyName("eventId")] Guid EventId,
    [property: JsonPropertyName("initialTickets")] int InitialTickets
);