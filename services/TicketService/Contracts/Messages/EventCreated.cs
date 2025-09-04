using System;
using System.Text.Json.Serialization;

namespace TicketService.Contracts.Messages;

public record EventCreated(
    [property: JsonPropertyName("eventId")] long EventId,
    [property: JsonPropertyName("initialTickets")] int InitialTickets
);