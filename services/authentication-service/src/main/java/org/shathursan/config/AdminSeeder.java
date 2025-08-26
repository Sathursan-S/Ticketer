package org.shathursan.config;

import java.util.Set;
import lombok.RequiredArgsConstructor;
import org.shathursan.entity.Role;
import org.shathursan.entity.User;
import org.shathursan.repository.UserRepository;
import org.springframework.boot.CommandLineRunner;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.crypto.password.PasswordEncoder;

@Configuration
@RequiredArgsConstructor
public class AdminSeeder {
  private final UserRepository users;
  private final PasswordEncoder encoder;

  @Bean
  CommandLineRunner seedAdmin() {
    return args -> {
      String adminEmail = "admin@gmail.com";
      if (users.existsByEmail(adminEmail)) return;

      var admin = User.builder()
          .email(adminEmail)
          .password(encoder.encode("admin123"))
          .roles(Set.of(Role.ADMIN))
          .build();

      users.save(admin);
    };
  }
}
