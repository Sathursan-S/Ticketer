package org.shathursan.messaging;

import lombok.RequiredArgsConstructor;
import org.shathursan.config.RabbitConfig;
import org.shathursan.contracts.EventCreated;
import org.shathursan.dto.request.NotificationRequest;
import org.shathursan.service.NotificationService;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.stereotype.Component;

@Component
@RequiredArgsConstructor
public class EventListener {
private final NotificationService notificationService;

@RabbitListener(queues = RabbitConfig.Q_EVENT_CREATED)
  public void handleEventCreated(EventCreated event) {
    // Logic to handle the event and send notification
    notificationService.sendNotification(new NotificationRequest(
        event.getUserEmail(),
        "Event Created",
        "Your event '" + event.getEventName() + "' (ID: " + event.getEventId() + ") has been created successfully."
    ));
  }
}
