package org.shathursan.dto.request;

import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class NotificationRequest {

  private String recipient;
  private String messageSubject;
  private String messageBody;

}
