package org.shathursan.controller;


import java.util.UUID;
import lombok.RequiredArgsConstructor;
import org.shathursan.entity.User;
import org.shathursan.service.UserService;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/v1/users")
@RequiredArgsConstructor
public class UserController {

  private final UserService userService;

  @GetMapping("/me")
  public ResponseEntity<User> me() {
    return ResponseEntity.status(501).build();
  }

  @PreAuthorize("hasRole('ADMIN')")
  @PostMapping("/{id}/roles")
  public ResponseEntity<Void> assignRole(@PathVariable("id") Long id, @RequestParam String role) {
    userService.assignRole(id, role);
    return ResponseEntity.status(501).build();
  }

}
