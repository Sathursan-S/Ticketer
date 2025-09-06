package org.shathursan.config;

import org.springframework.amqp.core.Queue;
import org.springframework.amqp.core.TopicExchange;
import org.springframework.amqp.rabbit.connection.ConnectionFactory;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.amqp.support.converter.Jackson2JsonMessageConverter;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class RabbitConfig {
  public static final String EXCHANGE = "events.exchange";
  public static final String RK_EVENT_CREATED = "event.created";
  public static final String RK_EVENT_DELETED = "event.deleted";
  public static final String RK_EVENT_UPDATED = "event.updated";
  public static final String RK_EVENT_PUBLISHED = "event.published";
  // For ticket creation
  public static final String RK_CREATE_EVENT_TICKET = "event.ticket.create";

  public static final String Q_EVENT_CREATED = "notificationEventCreatedQueue";
  public static final String Q_EVENT_DELETED = "notificationEventDeletedQueue";
  public static final String Q_EVENT_UPDATED = "notificationEventUpdatedQueue";
  public static final String Q_EVENT_PUBLISHED = "notificationEventPublishedQueue";
  // For ticket creation
  public static final String Q_CREATE_EVENT_TICKET = "create-event-ticket";
  @Bean
  public Queue notificationEventCreatedQueue() {
    return new Queue(Q_EVENT_CREATED, true, false, false);
  }

  @Bean
  public Queue createEventTicketQueue() {
    return new Queue(Q_CREATE_EVENT_TICKET, true, false, false);
  }

  @Bean
  public Queue notificationEventDeletedQueue() {
    return new Queue(Q_EVENT_DELETED, true, false, false);
  }

  @Bean
  public Queue notificationEventUpdatedQueue() {
    return new Queue(Q_EVENT_UPDATED, true, false, false);
  }

  @Bean
  public Queue notificationEventPublishedQueue() {
    return new Queue(Q_EVENT_PUBLISHED, true, false, false);
  }

  @Bean
  public TopicExchange eventsExchange() {
    return new TopicExchange(EXCHANGE, true, false);
  }

  // Bind the ticket queue to the exchange with the routing key
  @Bean
  public org.springframework.amqp.core.Binding createEventTicketBinding() {
    return org.springframework.amqp.core.BindingBuilder
      .bind(createEventTicketQueue())
      .to(eventsExchange())
      .with(RK_CREATE_EVENT_TICKET);
  }

  @Bean
  public Jackson2JsonMessageConverter jackson2JsonMessageConverter() {
    return new Jackson2JsonMessageConverter();
  }

  @Bean
  public RabbitTemplate rabbitTemplate(ConnectionFactory cf) {
    RabbitTemplate tpl = new RabbitTemplate(cf);
    tpl.setMessageConverter(jackson2JsonMessageConverter());
    return tpl;
  }

}