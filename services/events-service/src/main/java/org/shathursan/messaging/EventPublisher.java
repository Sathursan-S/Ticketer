package org.shathursan.messaging;

import lombok.RequiredArgsConstructor;
import org.shathursan.config.RabbitConfig;
import org.shathursan.contracts.EventCreated;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.stereotype.Component;

@Component
@RequiredArgsConstructor
public class EventPublisher {
  private final RabbitTemplate rabbitTemplate;

  public void publishEventCreated(EventCreated payload) {
    rabbitTemplate.convertAndSend(RabbitConfig.EXCHANGE, RabbitConfig.RK_EVENT_CREATED, payload);
  }
}