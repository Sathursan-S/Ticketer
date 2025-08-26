package org.shathursan.dto.response;

import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class ContentResponse<T> {
  private String status;
  private String statusCode;
  private String message;
  private String type;
  private T data;
  private String notification;

  public ContentResponse(String type, T data, String status, String statusCode, String notification, String message) {
    this.type = type;
    this.data = data;
    this.status = status;
    this.statusCode = statusCode;
    this.message = message;
    this.notification = notification;

  }
}
