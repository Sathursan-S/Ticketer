package org.shathursan.service;

import io.jsonwebtoken.Claims;
import java.time.OffsetDateTime;
import java.util.List;
import lombok.RequiredArgsConstructor;
import org.shathursan.dto.VenueDto;
import org.shathursan.dto.request.EventRequest;
import org.shathursan.dto.request.EventUpdateRequest;
import org.shathursan.dto.response.EventResponse;
import org.shathursan.entity.Event;
import org.shathursan.entity.VenueInfo;
import org.shathursan.entity.enums.EventCategory;
import org.shathursan.entity.enums.EventStatus;
import org.shathursan.repository.EventRepository;
import org.shathursan.messaging.MessageProducer;
import org.shathursan.contracts.CreateEventTicket;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.client.RestTemplate;

@Service
@RequiredArgsConstructor
@Transactional
public class EventServiceImpl implements EventService {

  private final EventRepository eventRepository;
  private final MessageProducer messageProducer;

  @Override
  public Event createEvent(EventRequest eventRequest) {
    try {
      var entity = Event.builder()
          .eventName(eventRequest.getEventName())
          .category(eventRequest.getCategory())
          .description(eventRequest.getDescription())
          .startAt(eventRequest.getStartAt())
          .duration(eventRequest.getDuration())
          .ticketPrice(eventRequest.getTicketPrice())
          .ticketsSold(0)
          .ticketCapacity(eventRequest.getTicketCapacity())
          .salesStartAt(eventRequest.getSalesStartAt())
          .salesEndAt(eventRequest.getSalesEndAt())
          .venue(eventRequest.getVenueDto().toEntity())
          .status(EventStatus.PENDING)
          .build();
      eventRepository.save(entity);
      // Minimal: send CreateEventTicket message after event creation
      CreateEventTicket ticketMsg = CreateEventTicket.builder()
        .eventId(entity.getId())
        .numberOfTickets(entity.getTicketCapacity())
        .build();
      messageProducer.sendCreateEventTicket(ticketMsg);
      return entity;
    } catch (Exception e) {
      throw new RuntimeException("Failed to create event: " + e.getMessage(), e);
    }
  }

  @Override
  public Event update(Long eventId, EventUpdateRequest eventRequest) {
    var event = eventRepository.findById(eventId)
        .orElseThrow(() -> new IllegalArgumentException("Event not found"));
    if (event.getStatus() == EventStatus.CANCELLED) {
      throw new IllegalArgumentException("Cancelled events cannot be updated");
    }
    var entity = Event.builder()
        .id(event.getId())
        .eventName(eventRequest.getEventName())
        .category(eventRequest.getCategory())
        .description(eventRequest.getDescription())
        .status(eventRequest.getStatus())
        .startAt(eventRequest.getStartAt())
        .duration(eventRequest.getDuration())
        .ticketPrice(eventRequest.getTicketPrice())
        .ticketsSold(event.getTicketsSold())
        .ticketCapacity(eventRequest.getTicketCapacity())
        .salesStartAt(eventRequest.getSalesStartAt())
        .salesEndAt(eventRequest.getSalesEndAt())
        .venue(eventRequest.getVenueDto().toEntity())
        .status(eventRequest.getStatus())
        .build();
    eventRepository.save(entity);
    return entity;
  }

  @Override
  public void publish(Long eventId) {
    Event event = eventRepository.findById(eventId)
        .orElseThrow(() -> new IllegalArgumentException("Event not found"));
    if (event.getStatus() == EventStatus.CANCELLED) {
      throw new IllegalArgumentException("Cancelled events cannot be published");
    }
    if (event.getStatus() == EventStatus.ACTIVE) {
      throw new IllegalArgumentException("Event is already published");
    }
    event.setStatus(EventStatus.ACTIVE);
    eventRepository.save(event);
  }

  @Override
  public void cancel(Long eventId) {
    var entity = eventRepository.findById(eventId)
        .orElseThrow(() -> new IllegalArgumentException("Event not found"));
    if (entity.getStatus() == EventStatus.CANCELLED) {
      throw new IllegalArgumentException("Event is already cancelled");
    }
    entity.setStatus(EventStatus.CANCELLED);
  }

  @Override
  @Transactional
  public EventResponse getEventById(Long eventId) {
    var event = eventRepository.findById(eventId)
        .orElseThrow(() -> new IllegalArgumentException("Event not found"));
    if (event.getStatus() != EventStatus.ACTIVE ) {
      throw new IllegalArgumentException("Event not published");
    }
    return toResponse(event);
  }

  @Override
  @Transactional
  public EventResponse getEventWithAllStatusById(Long eventId) {
    var event = eventRepository.findById(eventId)
        .orElseThrow(() -> new IllegalArgumentException("Event not found"));
    return toResponse(event);
  }


  @Override
  @Transactional
  public Page<EventResponse> listPublic(String category, String location, String date, String q,
      Pageable pageable) {
    EventStatus status = EventStatus.ACTIVE;
    EventCategory eventCategory = null;
    if (category != null && !category.isBlank()) {
      try {
        eventCategory = EventCategory.valueOf(category);
      } catch (Exception ignored) {
      }
    }
    OffsetDateTime from =
        date != null ? OffsetDateTime.parse(date) : OffsetDateTime.now().minusYears(1);
    OffsetDateTime to = OffsetDateTime.now().plusYears(1);

    Page<Event> events;
    if (eventCategory != null) {
      events = eventRepository.findByStatusAndCategoryAndStartAtBetween(status, eventCategory, from, to, pageable);
    } else {
      Page<Event> allEvents = eventRepository.findAll(pageable);
      List<Event> filteredEvents = allEvents.getContent().stream()
          .filter(event -> event.getStatus() == status
              && !event.getStartAt().isBefore(from)
              && !event.getStartAt().isAfter(to))
          .toList();
      events = new org.springframework.data.domain.PageImpl<>(filteredEvents, pageable, allEvents.getTotalElements());
    }
    return events.map(this::toResponse);
  }

  @Override
  public List<EventResponse> listAll() {
    List<Event> events = eventRepository.findAll();
    return events.stream().map(this::toResponse).toList();
  }

  private EventResponse toResponse(Event entity) {
    int availableTickets = Math.max(0, entity.getTicketCapacity() - entity.getTicketsSold());
    return EventResponse.builder()
        .id(entity.getId())
        .eventName(entity.getEventName())
        .category(entity.getCategory())
        .status(entity.getStatus())
        .description(entity.getDescription())
        .startAt(entity.getStartAt())
        .duration(entity.getDuration())
        .ticketPrice(entity.getTicketPrice())
        .ticketCapacity(entity.getTicketCapacity())
        .ticketsSold(entity.getTicketsSold())
        .ticketsAvailable(availableTickets)
        .salesStartAt(entity.getSalesStartAt())
        .salesEndAt(entity.getSalesEndAt())
        .venueDto(entity.getVenue() == null ? null : toVenueDto(entity.getVenue()))
        .createdAt(entity.getCreatedAt())
        .updatedAt(entity.getUpdatedAt())
        .build();
  }

  private VenueDto toVenueDto(VenueInfo venue) {
    var dto = new VenueDto();
    dto.setName(venue.getName());
    dto.setAddressLine1(venue.getAddressLine1());
    dto.setAddressLine2(venue.getAddressLine2());
    dto.setCity(venue.getCity());
    dto.setCountry(venue.getCountry());
    return dto;
  }


  private Boolean isUserExist(String userEmail, Long organizerId) {
    // Example endpoint: /api/v1/users/exists?email=...&organizerId=...
    String url = "http://authentication-service/api/v1/users/exists?email=" + userEmail + "&organizerId=" + organizerId;
    try {
      RestTemplate restTemplate = new RestTemplate();
      ResponseEntity<Boolean> response = restTemplate.getForEntity(url, Boolean.class);
      return response.getBody() != null && response.getBody();

    } catch (Exception e) {
      // Log error or handle as needed
      return false;
    }
  }


  private static void organizerDetails() {
    var authentication = SecurityContextHolder.getContext().getAuthentication();
    if (authentication == null){
      throw new RuntimeException("Authentication not found");
    }
    Claims claims = (Claims) authentication.getDetails();
    String userEmail = claims.getSubject();
    String userId = claims.get("uid", String.class);
    System.out.println("User ID from JWT: " + userId);
    System.out.println("User Email from JWT: " + userEmail);
  }

}
