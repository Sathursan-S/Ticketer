package org.shathursan.messaging;

import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.shathursan.config.RabbitConfig;
import org.shathursan.contracts.EventCreated;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.stereotype.Component;

@Slf4j
@Component
@RequiredArgsConstructor
public class EventPublisher {

  private final RabbitTemplate rabbitTemplate;

  /**
   * Publishes an EventCreated message to the configured RabbitMQ exchange.
   *
   * @param payload the event data to publish
   */
  public void publishEventCreated(EventCreated payload) {
    rabbitTemplate.convertAndSend(
      RabbitConfig.EXCHANGE,
      RabbitConfig.RK_EVENT_CREATED,
      payload
    );
  }

  public void publishEventDeleted(EventCreated payload) {
    rabbitTemplate.convertAndSend(
      RabbitConfig.EXCHANGE,
      RabbitConfig.RK_EVENT_DELETED,
      payload
    );
    log.info("EventDeleted message sent for eventId={}", payload.getEventId());
  }

  public void publishEventUpdated(EventCreated payload) {
    rabbitTemplate.convertAndSend(
      RabbitConfig.EXCHANGE,
      RabbitConfig.RK_EVENT_UPDATED,
      payload
    );
    log.info("EventUpdated message sent for eventId={}", payload.getEventId());
  }

  public void publishEventPublished(EventCreated payload) {
    rabbitTemplate.convertAndSend(
      RabbitConfig.EXCHANGE,
      RabbitConfig.RK_EVENT_PUBLISHED,
      payload
    );
    log.info("EventPublished message sent for eventId={}", payload.getEventId());
  }
}