package org.shathursan.contracts;

import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;
import java.util.UUID;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class EventCreated {
  private UUID eventId;
  private int initialTickets;
}