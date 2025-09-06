package org.shathursan.contracts;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class EventCreated {
    private long eventId;
    private String eventName;
    private String userEmail;
    private int numberOfTickets;
}
