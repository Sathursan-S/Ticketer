package org.shathursan.service;

import java.time.LocalDateTime;

import lombok.RequiredArgsConstructor;
import org.shathursan.dto.request.NotificationRequest;
import org.shathursan.entity.Notification;
import org.shathursan.respository.NotificationRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

@Service
@RequiredArgsConstructor
public class NotificationService {

    private final NotificationRepository notificationRepository;
    private final EmailService emailService;
    private final Logger log = LoggerFactory.getLogger(NotificationService.class);

    public Notification sendNotification(NotificationRequest request) {
        Notification notification = Notification.builder()
                .recipient(request.getRecipient())
                .messageSubject(request.getMessageSubject())
                .messageBody(request.getMessageBody())
                .sentAt(LocalDateTime.now())
                .build();

        try {
            emailService.sendEmail(notification.getRecipient(), notification.getMessageSubject(),
                    notification.getMessageBody());
            log.info("Email sent to {}", notification.getRecipient());
        } catch (Exception e) {
            log.error("Failed to send email to {}", notification.getRecipient(), e);
        }
        return notificationRepository.save(notification);
    }
}
