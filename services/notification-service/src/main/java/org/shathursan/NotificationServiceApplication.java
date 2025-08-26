package org.shathursan;

import org.springframework.boot.autoconfigure.SpringBootApplication;

@SpringBootApplication
public class NotificationServiceApplication {

  public static void main(String[] args) {
    org.springframework.boot.SpringApplication.run(NotificationServiceApplication.class, args);
    System.out.println(" Notification Service is running... ");
  }
}