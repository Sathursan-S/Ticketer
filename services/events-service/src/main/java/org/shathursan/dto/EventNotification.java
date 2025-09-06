package org.shathursan.dto;

import java.time.LocalDateTime;
import lombok.AllArgsConstructor;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@AllArgsConstructor
@NoArgsConstructor
public class EventNotification {
  private String eventId;
  private String eventType; // "CREATED", "DELETED", "PUBLISHED"
  private String eventName;
  private String description;
  private LocalDateTime timestamp;
}
