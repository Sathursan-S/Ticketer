package org.shathursan.dto.request;

import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Positive;
import jakarta.validation.constraints.PositiveOrZero;
import java.time.OffsetDateTime;
import lombok.Data;
import org.shathursan.dto.VenueDto;
import org.shathursan.entity.enums.EventCategory;
import org.shathursan.entity.enums.EventStatus;

@Data
public class EventUpdateRequest {

  @NotBlank
  private String eventName;

  @NotNull
  private EventCategory category;

  @NotNull
  private EventStatus status;

  private String description;

  @NotNull
  private OffsetDateTime startAt;

  @NotNull
  @Positive
  private Integer duration;

  @NotNull
  @PositiveOrZero
  private Double ticketPrice;

  @NotNull
  @Positive
  private Integer ticketCapacity;

  private OffsetDateTime salesStartAt;
  private OffsetDateTime salesEndAt;

  @Valid
  @NotNull
  private VenueDto venueDto;

}
