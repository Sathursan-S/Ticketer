package org.shathursan.repository;

import java.time.OffsetDateTime;
import java.util.List;
import org.shathursan.entity.Event;
import org.shathursan.entity.enums.EventCategory;
import org.shathursan.entity.enums.EventStatus;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

public interface EventRepository extends JpaRepository<Event, Long> {
  Page<Event> findByStatusAndCategoryAndStartAtBetween(
      EventStatus status, EventCategory category, OffsetDateTime from, OffsetDateTime to, Pageable pageable);

  Long findOrganizerIdById(Long eventId);
  @Query("SELECT e.eventName FROM Event e WHERE e.id = :id")
  String findEventNameById(@Param("id") Long eventId);


}
