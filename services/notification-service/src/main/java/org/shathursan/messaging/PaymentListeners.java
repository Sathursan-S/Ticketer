package org.shathursan.messaging;

import org.shathursan.config.RabbitConfig;
import org.shathursan.contracts.PaymentFailed;
import org.shathursan.contracts.PaymentSucceeded;
import org.shathursan.service.NotificationService;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.messaging.handler.annotation.Payload;
import org.springframework.stereotype.Component;

@Component
public class PaymentListeners {
  private static final Logger log = LoggerFactory.getLogger(PaymentListeners.class);
  private final NotificationService svc;

  public PaymentListeners(NotificationService svc) { this.svc = svc; }

  @RabbitListener(queues = RabbitConfig.Q_SUCCESS)
  public void onPaymentSucceeded(@Payload PaymentSucceeded m) {
    log.info(" Payment Succeeded booking={} customer={} amount={} {}",
        m.getBookingId(), m.getCustomerId(), m.getAmount(), m.getCurrency());
    svc.paymentSucceeded(
        m.getBookingId(), m.getCustomerId(),
        m.getPaymentIntentId(), m.getAmount(), m.getCurrency(), m.getPaidAtUtc()
    );
  }

  @RabbitListener(queues = RabbitConfig.Q_FAILED)
  public void onPaymentFailed(@Payload PaymentFailed m) {
    log.warn(" Payment Failed booking={} customer={} reason={}",
        m.getBookingId(), m.getCustomerId(), m.getReason());
    svc.paymentFailed(
        m.getBookingId(), m.getCustomerId(), m.getReason(), m.getFailedAtUtc()
    );
  }
}