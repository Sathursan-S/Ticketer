package org.shathursan.dto.request;

import lombok.Data;

@Data
public class NotificationRequest {

  private String recipient;
  private String messageSubject;
  private String messageBody;

  public NotificationRequest(String recipient, String messageSubject, String messageBody) {
    this.recipient = recipient;
    this.messageSubject = messageSubject;
    this.messageBody = messageBody;
  }

}
