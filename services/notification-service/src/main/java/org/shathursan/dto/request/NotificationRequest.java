package org.shathursan.dto.request;

import lombok.Data;

@Data
public class NotificationRequest {

  private String recipient;
  private String messageSubject;
  private String messageBody;

}
