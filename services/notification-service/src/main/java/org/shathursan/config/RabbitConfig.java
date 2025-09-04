package org.shathursan.config;

import org.springframework.amqp.core.*;
import org.springframework.amqp.rabbit.connection.ConnectionFactory;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.amqp.rabbit.config.SimpleRabbitListenerContainerFactory;
import org.springframework.amqp.support.converter.Jackson2JsonMessageConverter;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class RabbitConfig {
  public static final String EXCHANGE   = "payment-events";
  public static final String RK_SUCCESS = "payment.succeeded";
  public static final String RK_FAILED  = "payment.failed";
  public static final String Q_SUCCESS  = "notification.payment.succeeded.q";
  public static final String Q_FAILED   = "notification.payment.failed.q";

  @Bean TopicExchange paymentExchange() {
    return ExchangeBuilder.topicExchange(EXCHANGE).durable(true).build();
  }
  @Bean Queue successQueue() { return QueueBuilder.durable(Q_SUCCESS).build(); }
  @Bean Queue failedQueue()  { return QueueBuilder.durable(Q_FAILED).build(); }

  @Bean Binding bindSuccess(Queue successQueue, TopicExchange paymentExchange) {
    return BindingBuilder.bind(successQueue).to(paymentExchange).with(RK_SUCCESS);
  }
  @Bean Binding bindFailed(Queue failedQueue, TopicExchange paymentExchange) {
    return BindingBuilder.bind(failedQueue).to(paymentExchange).with(RK_FAILED);
  }

  @Bean Jackson2JsonMessageConverter jackson2JsonMessageConverter() {
    return new Jackson2JsonMessageConverter();
  }

  // Make @RabbitListener methods use JSON
  @Bean SimpleRabbitListenerContainerFactory rabbitListenerContainerFactory(
      ConnectionFactory cf, Jackson2JsonMessageConverter converter) {
    var f = new SimpleRabbitListenerContainerFactory();
    f.setConnectionFactory(cf);
    f.setMessageConverter(converter);
    return f;
  }

  // Optional: template with JSON converter (handy if you later publish)
  @Bean RabbitTemplate rabbitTemplate(ConnectionFactory cf, Jackson2JsonMessageConverter conv) {
    var tpl = new RabbitTemplate(cf);
    tpl.setMessageConverter(conv);
    return tpl;
  }
}