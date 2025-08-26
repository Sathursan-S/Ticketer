package org.shathursan;

import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

@SpringBootApplication
public class AuthenticationServiceApplication {
  private static final Logger logger = LoggerFactory.getLogger(AuthenticationServiceApplication.class);

  public static void main(String[] args) {
    org.springframework.boot.SpringApplication.run(AuthenticationServiceApplication.class, args);
    logger.info("Authentication Service is running...");
  }

}
