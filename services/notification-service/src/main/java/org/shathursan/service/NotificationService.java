package org.shathursan.service;

import org.shathursan.entity.Notification;
import org.shathursan.repository.NotificationRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.time.LocalDateTime;
import java.time.ZoneOffset;
import java.util.UUID;

@Service
public class NotificationService {
  private final NotificationRepository repo;
  public NotificationService(NotificationRepository repo) { this.repo = repo; }

  @Transactional
  public void paymentSucceeded(UUID bookingId, UUID customerId, String paymentIntentId,
                               double amount, String currency, java.time.Instant paidAtUtc) {
    var n = Notification.builder()
        .recipient(customerId.toString())
        .messageSubject("Payment succeeded")
        .messageBody("Booking " + bookingId + " paid " + amount + " " + currency +
                     " (intent " + paymentIntentId + ")")
        .sentAt(LocalDateTime.now(ZoneOffset.UTC))
        .build();
    repo.save(n);
  }

  @Transactional
  public void paymentFailed(UUID bookingId, UUID customerId, String reason,
                            java.time.Instant failedAtUtc) {
    var n = Notification.builder()
        .recipient(customerId.toString())
        .messageSubject("Payment failed")
        .messageBody("Booking " + bookingId + " failed: " + reason)
        .sentAt(LocalDateTime.now(ZoneOffset.UTC))
        .build();
    repo.save(n);
  }
}