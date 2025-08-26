package org.shathursan.service;

import org.shathursan.dto.request.EventRequest;
import org.shathursan.dto.request.EventUpdateRequest;
import org.shathursan.dto.response.EventResponse;
import org.shathursan.entity.Event;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.Pageable;

import java.util.List;

public interface EventService {

  Event createEvent(EventRequest req);
  Event update(Long eventId, EventUpdateRequest req);

  void publish(Long eventId);
  void cancel(Long eventId);

//  EventResponse getPublicEvent(Long eventId);
  Page<EventResponse> listPublic(String category, String location, String date, String q, Pageable pageable);
  List<EventResponse> listAll();

  EventResponse getEventById(Long id);
  EventResponse getEventWithAllStatusById(Long id);
}
