package org.shathursan.entity;

import jakarta.persistence.Column;
import jakarta.persistence.Embedded;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.Id;
import jakarta.persistence.Table;
import java.time.Instant;
import java.time.OffsetDateTime;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;
import org.hibernate.annotations.CreationTimestamp;
import org.hibernate.annotations.UpdateTimestamp;
import org.shathursan.entity.enums.EventCategory;
import org.shathursan.entity.enums.EventStatus;

@Entity
@Data
@Table(name = "events")
@AllArgsConstructor
@NoArgsConstructor
@Builder
public class Event {

  @Id
  @GeneratedValue(strategy = GenerationType.AUTO)
  private Long id;

//  @Column(nullable = false)
//  private Long organizerId;

//  @Column(nullable = false)
//  private String organnizerMailId;

  @Column(nullable = false)
  private String eventName;

  @Enumerated(EnumType.STRING)
  @Column(nullable = false)
  private EventCategory category;

  @Column(length = 2000)
  private String description;

  @Column(nullable = false)
  private OffsetDateTime startAt;

  @Column(nullable = false)
  private Integer duration;

  @Column(nullable = false)
  private Double ticketPrice;

  @Column(nullable = false)
  private int ticketCapacity;
  // total sellable tickets
  @Column(nullable = false)
  private int ticketsSold;              // update via booking service callbacks/webhooks

  private OffsetDateTime salesStartAt;
  private OffsetDateTime salesEndAt;

  @Embedded
  private VenueInfo venue;

  @CreationTimestamp
  private Instant createdAt;
  @UpdateTimestamp
  private Instant updatedAt;

  //status
  @Enumerated(EnumType.STRING)
  @Column(nullable = false)
  private EventStatus status;

}
