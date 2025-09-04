package org.shathursan.controller;

import lombok.RequiredArgsConstructor;
import org.shathursan.dto.request.NotificationRequest;
import org.shathursan.dto.response.NotificationResponse;
import org.shathursan.entity.Notification;
import org.shathursan.service.NotificationService;
import org.shathursan.utils.ApiEndpoints;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/v1/notifications")
@RequiredArgsConstructor
public class NotificationController {

    private final NotificationService notificationService;

    @PostMapping(ApiEndpoints.GENERATE)
    public ResponseEntity<NotificationResponse> sendNotification(@RequestBody NotificationRequest request) {
        try {
            Notification notification = notificationService.sendNotification(request);
            NotificationResponse response = new NotificationResponse(
                    200,
                    "Notification sent successfully",
                    notification
            );
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            NotificationResponse errorResponse = new NotificationResponse(
                    500,
                    "Failed to send notification: " + e.getMessage(),
                    null
            );
            return ResponseEntity.status(500).body(errorResponse);
        }
    }
}
