package org.shathursan.repository;

import java.util.Optional;
import java.util.UUID;
import org.shathursan.entity.Role;
import org.shathursan.entity.User;
import org.springframework.data.jpa.repository.JpaRepository;


public interface UserRepository extends JpaRepository<User, Long> {
  Optional<User> findByEmail(String email);
  boolean existsByEmail(String email);
}
