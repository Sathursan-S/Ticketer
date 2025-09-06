package org.shathursan.messaging;

import org.shathursan.config.RabbitConfig;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.stereotype.Service;

@Service
public class MessageProducer {

  private  final RabbitTemplate rabbitTemplate;

  public MessageProducer(RabbitTemplate rabbitTemplate) {
    this.rabbitTemplate = rabbitTemplate;
  }

  public void sendMessage(String routingKey, Object message) {
    rabbitTemplate.convertAndSend(RabbitConfig.Q_EVENT_CREATED, message);
  }
}
