package org.shathursan.controller;

import io.jsonwebtoken.Claims;
import jakarta.validation.Valid;
import java.net.URI;
import java.util.List;
import lombok.RequiredArgsConstructor;
import org.shathursan.client.NotificationClient;
import org.shathursan.dto.OrganizerDetails;
import org.shathursan.dto.request.EventRequest;
import org.shathursan.dto.request.EventUpdateRequest;
import org.shathursan.dto.response.ContentResponse;
import org.shathursan.dto.response.EventResponse;
import org.shathursan.entity.Event;
import org.shathursan.repository.EventRepository;
import org.shathursan.service.EventService;
import org.shathursan.util.ApiEndpoints;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.client.RestTemplate;
import org.springframework.web.util.UriComponentsBuilder;

@RestController
@RequestMapping("/api/v1/organizer/events")
@RequiredArgsConstructor
public class OrganizerEventController {

  private final EventService eventService;
  private final NotificationClient notificationClient;
  private final EventRepository eventRepository;

  @PreAuthorize("hasRole('ORGANIZER')")
  @PostMapping(ApiEndpoints.ADD_EVENT)
  public ResponseEntity<ContentResponse<Event>> create(@Valid @RequestBody EventRequest request) {
    isUserExist(organizerDetails().getUserEmail());
    Event event = eventService.createEvent(request);
    String notificationStatus;
    String message = "Event created successfully.";
    try {
      notificationClient.sendNotification(
          organizerDetails().getUserEmail(), "Event Created",
          "Your event " + event.getEventName() + " has been created successfully."
      );
      notificationStatus = "success";
    } catch (Exception e) {
      notificationStatus = "failure";
      message += " Notification failed: " + e.getMessage();
    }
    ContentResponse<Event> response = new ContentResponse<>(
        "Event",
        event,
        "success",
        notificationStatus,
        "201",
        "Event created successfully."
    );
    return ResponseEntity.status(201).body(response);
  }


  @PreAuthorize("hasRole('ORGANIZER')")
  @PutMapping(ApiEndpoints.UPDATE_EVENT)
  public ResponseEntity<ContentResponse<Event>> update(
      @PathVariable Long id,
      @Valid @RequestBody EventUpdateRequest request) {
    try {
      isUserExist(organizerDetails().getUserEmail());
      Event event = eventService.update(id, request);
      isUserExist(organizerDetails().getUserEmail());
      String notificationStatus;
      String message = "Event updated successfully.";
      try {
        notificationClient.sendNotification(
            organizerDetails().getUserEmail(), "Event Updated",
            "Your event " + event.getEventName() + " has been Updated successfully."
        );
        notificationStatus = "success";
      } catch (Exception e) {
        notificationStatus = "failure";
        message += " Notification failed: " + e.getMessage();
      }

      ContentResponse<Event> response = new ContentResponse<>(
          "Event",
          event,
          "success",
          notificationStatus,
          "200",
          "Event updated successfully."
      );
      return ResponseEntity.ok(response);
    } catch (Exception e) {
      ContentResponse<Event> errorResponse = new ContentResponse<>(
          "Event",
          null,
          "error",
          "400",
          "Event not found",
          "Event update failed: " + e.getMessage()
      );
      return ResponseEntity.badRequest().body(errorResponse);
    }
  }

  @PreAuthorize("hasRole('ORGANIZER')")
  @PutMapping(ApiEndpoints.PUBLISH_EVENT)
  public ResponseEntity<ContentResponse<Void>> publish(@PathVariable Long id) {
    try {
      isUserExist(organizerDetails().getUserEmail());
      eventService.publish(id);
      String notificationStatus;
      String message = "Event created successfully.";
      try {
        notificationClient.sendNotification(
            organizerDetails().getUserEmail(), "Event published",
            "Your event " + eventRepository.findEventNameById(id)
                + " has been published successfully."
        );
        notificationStatus = "success";
      } catch (Exception e) {
        notificationStatus = "failure";
        message += " Notification failed: " + e.getMessage();
        e.printStackTrace();
      }
      ContentResponse<Void> response = new ContentResponse<>(
          "Event",
          null,
          "success",
          "200",
          notificationStatus,
          "Event published successfully."
      );
      return ResponseEntity.ok(response);
    } catch (Exception e) {
      ContentResponse<Void> errorResponse = new ContentResponse<>(
          "Event",
          null,
          "error",
          "400",
          "Notification failed",
          "Event publish failed: " + e.getMessage()
      );
      return ResponseEntity.badRequest().body(errorResponse);
    }
  }

  @PreAuthorize("hasRole('ORGANIZER')")
  @PutMapping(ApiEndpoints.CANCEL_EVENT)
  public ResponseEntity<ContentResponse<Void>> cancel(@PathVariable Long id) {
    try {
      isUserExist(organizerDetails().getUserEmail());
      eventService.cancel(id);
      String notificationStatus;
      String message = "Event cancelled successfully.";

      try {
        notificationClient.sendNotification(
            organizerDetails().getUserEmail(), "Event cancelled",
            "Your event " + eventRepository.findEventNameById(id)
                + " has been cancelled successfully."
        );
        notificationStatus = "success";
      } catch (Exception e) {
        notificationStatus = "failure";
        message += " Notification failed: " + e.getMessage();
      }
      ContentResponse<Void> response = new ContentResponse<>(
          "Event",
          null,
          "success",
          "200",
          notificationStatus,
          "Event cancelled successfully."
      );
      return ResponseEntity.ok(response);
    } catch (Exception e) {
      ContentResponse<Void> errorResponse = new ContentResponse<>(
          "Event",
          null,
          "error",
          "400",
          "Notification failed",
          "Event cancel failed: " + e.getMessage()
      );
      return ResponseEntity.badRequest().body(errorResponse);
    }
  }

  @PreAuthorize("hasRole('ORGANIZER')")
  @GetMapping(ApiEndpoints.EVENTS)
  public ResponseEntity<ContentResponse<List<EventResponse>>> getAllEvents() {
    isUserExist(organizerDetails().getUserEmail());
    List<EventResponse> events = eventService.listAll();
    ContentResponse<List<EventResponse>> response = new ContentResponse<>(
        "Events",
        events,
        "success",
        "200",
        "NO Needed",
        events.isEmpty() ? "No events found." : "Events fetched successfully."
    );
    return ResponseEntity.ok(response);
  }

  @GetMapping(ApiEndpoints.EVENT_BY_ID)
  public ResponseEntity<ContentResponse<EventResponse>> getEventById(@PathVariable Long id
  ) {
    try {

      isUserExist(organizerDetails().getUserEmail());
      EventResponse event = eventService.getEventWithAllStatusById(id);
      ContentResponse<EventResponse> response = new ContentResponse<>(
          "Event",
          event,
          "success",
          "200",
          "NO Needed",
          "Event Get successfully."
      );
      return ResponseEntity.ok(response);
    } catch (Exception e) {
      ContentResponse<EventResponse> errorResponse = new ContentResponse<>(
          "Event",
          null,
          "error",
          "404",
          "NO Needed",
          "Event not found: " + e.getMessage()
      );
      return ResponseEntity.status(404).body(errorResponse);
    }
  }


  private OrganizerDetails organizerDetails() {
    var authentication = SecurityContextHolder.getContext().getAuthentication();
    if (authentication == null) {
      throw new RuntimeException("Authentication not found");
    }
    Claims claims = (Claims) authentication.getDetails();
    String userEmail = claims.getSubject();
    String userId = claims.get("uid", String.class);
    System.out.println("User ID from JWT: " + userId);
    System.out.println("User Email from JWT: " + userEmail);
    return new OrganizerDetails(userId, userEmail);
  }

  private Boolean isUserExist(String userEmail) {
    URI uri = UriComponentsBuilder.fromUriString("http://localhost:8082/api/v1/auth/exists")
        .queryParam("mailId", userEmail)
        .build()
        .toUri();
    try {
      RestTemplate restTemplate = new RestTemplate();
      ResponseEntity<Boolean> response = restTemplate.getForEntity(uri, Boolean.class);
      return response.getBody() != null && response.getBody();
    } catch (Exception e) {
      e.printStackTrace();
      return false;
    }
  }
}
