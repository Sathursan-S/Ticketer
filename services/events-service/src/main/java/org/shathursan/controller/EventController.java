package org.shathursan.controller;

import java.util.List;
import lombok.RequiredArgsConstructor;
import org.shathursan.dto.response.ContentResponse;
import org.shathursan.dto.response.EventResponse;
import org.shathursan.service.EventService;
import org.shathursan.util.ApiEndpoints;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequiredArgsConstructor
@RequestMapping("/api/v1/events")
public class EventController {

  private final EventService eventService;

  @GetMapping
  public ResponseEntity<Page<EventResponse>> list(
      @RequestParam(required = false) String category,
      @RequestParam(required = false) String location,
      @RequestParam(required = false) String date,
      @RequestParam(required = false) String q) {
    return ResponseEntity.ok(eventService.listPublic(category, location, date, q, Pageable.unpaged()));
  }

  @GetMapping(ApiEndpoints.EVENT_BY_ID)
  public ResponseEntity<EventResponse> get(@PathVariable Long id) {
    return ResponseEntity.ok(eventService.getEventById(id));
  }

  @GetMapping("/all")
  public ResponseEntity<ContentResponse<List<EventResponse>>> getAllEvents() {
    List<EventResponse> events = eventService.listAll();
    ContentResponse<List<EventResponse>> response = new ContentResponse<>(
        "Events",
        events,
        "success",
        "200",
        "No Need",
        events.isEmpty() ? "No events found." : "Events fetched successfully."
    );
    return ResponseEntity.ok(response);
  }
}
