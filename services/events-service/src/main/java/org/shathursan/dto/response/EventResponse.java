package org.shathursan.dto.response;

import java.time.Duration;
import java.time.Instant;
import java.time.OffsetDateTime;
import lombok.Builder;
import lombok.Data;
import org.shathursan.dto.VenueDto;
import org.shathursan.entity.enums.EventCategory;
import org.shathursan.entity.enums.EventStatus;

@Data
@Builder
public class EventResponse {

  private Long id;
//  private Long organizerId;
  private String eventName;
  private EventCategory category;
  private EventStatus status;
  private String description;
  private OffsetDateTime startAt;
  private Integer duration;

  private Double ticketPrice;
  private int ticketCapacity;
  private int ticketsSold;
  private int ticketsAvailable;

  private OffsetDateTime salesStartAt;
  private OffsetDateTime salesEndAt;

  private VenueDto venueDto;

  private Instant createdAt;
  private Instant updatedAt;

}
