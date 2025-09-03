package org.shathursan.service;

import java.time.LocalDateTime;
import lombok.RequiredArgsConstructor;
import org.shathursan.dto.request.NotificationRequest;
import org.shathursan.entity.Notification;
import org.shathursan.respository.NotificationRepository;
import org.springframework.stereotype.Service;

@Service
@RequiredArgsConstructor
public class NotificationService {

  private final NotificationRepository notificationRepository;
  private final EmailService emailService;

  public Notification sendNotification(NotificationRequest request) {
    Notification notification = Notification.builder()
        .recipient(request.getRecipient())
        .messageSubject(request.getMessageSubject())
        .messageBody(request.getMessageBody())
        .sentAt(LocalDateTime.now())
        .build();

    emailService.sendEmail(notification.getRecipient(), notification.getMessageSubject(),
        notification.getMessageBody());
    return notificationRepository.save(notification);
  }
}
