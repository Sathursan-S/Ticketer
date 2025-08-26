package org.shathursan.dto.response;

import lombok.AllArgsConstructor;
import lombok.Data;
import org.shathursan.entity.Notification;

@Data
@AllArgsConstructor
public class NotificationResponse {
  private int status;
  private String message;
  private Notification notification;
}
