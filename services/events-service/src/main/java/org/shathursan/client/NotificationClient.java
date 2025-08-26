package org.shathursan.client;

import org.shathursan.dto.request.NotificationRequest;
import org.springframework.stereotype.Component;
import org.springframework.web.client.RestTemplate;

@Component
public class NotificationClient {
  private final RestTemplate restTemplate = new RestTemplate();
  private final String notificationServiceUrl = "http://localhost:8084/api/v1/notifications/generate";

  public void sendNotification(String recipient, String subject, String body) {
    NotificationRequest request = new NotificationRequest(recipient, subject, body);
    restTemplate.postForEntity(notificationServiceUrl, request, Void.class);
  }

}
