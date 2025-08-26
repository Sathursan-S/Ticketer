package org.shathursan;

import org.springframework.boot.autoconfigure.SpringBootApplication;

@SpringBootApplication
public class EventServiceApplication {

  public static void main(String[] args) {
    org.springframework.boot.SpringApplication.run(EventServiceApplication.class, args);
    System.out.println("Event Service is running...");
  }
}
