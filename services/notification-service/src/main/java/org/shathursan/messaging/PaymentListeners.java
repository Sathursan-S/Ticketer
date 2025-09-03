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
    svc.sendNotification(new NotificationRequest(
        m.getCustomerId().toString(),
        "Payment succeeded",
        "Booking " + m.getBookingId() + " paid " + m.getAmount() + " " + m.getCurrency() +
            " (intent " + m.getPaymentIntentId() + ")"
    ));
  }

  @RabbitListener(queues = RabbitConfig.Q_FAILED)
  public void onPaymentFailed(@Payload PaymentFailed m) {
    log.warn(" Payment Failed booking={} customer={} reason={}",
        m.getBookingId(), m.getCustomerId(), m.getReason());
    svc.sendNotification(new NotificationRequest(
        m.getCustomerId().toString(),
        "Payment failed",
        "Booking " + m.getBookingId() + " failed: " + m.getReason()
    ));
  }
}