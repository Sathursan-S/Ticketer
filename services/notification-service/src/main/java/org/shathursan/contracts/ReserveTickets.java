package org.shathursan.contracts;

import java.util.List;
import java.util.UUID;

import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;
import lombok.Builder;

@Data
@AllArgsConstructor

@Builder
public class ReserveTickets {
    private final UUID bookingId;
    private final long eventId;
    private final List<UUID> ticketIds;
    private final String customerId; 
}
