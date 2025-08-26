package org.shathursan.dto.request;

import lombok.AllArgsConstructor;
import lombok.Data;

@Data
@AllArgsConstructor
public class NotificationRequest {

  private String recipient;
  private String messageSubject;
  private String messageBody;
}
