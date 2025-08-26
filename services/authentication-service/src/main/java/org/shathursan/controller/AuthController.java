package org.shathursan.controller;

import jakarta.validation.Valid;
import lombok.RequiredArgsConstructor;
import org.shathursan.dto.LoginRequest;
import org.shathursan.dto.RefreshRequest;
import org.shathursan.dto.RegisterRequest;
import org.shathursan.dto.response.TokenResponse;
import org.shathursan.service.AuthService;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping("/api/v1/auth")
@RequiredArgsConstructor
public class AuthController {

  private final AuthService authService;

  @PostMapping("/register")
  public ResponseEntity<TokenResponse> register(@Valid @RequestBody RegisterRequest reg){
    return ResponseEntity.ok(authService.register(reg));
  }

  @PostMapping("/login")
  public ResponseEntity<TokenResponse> login(@Valid @RequestBody LoginRequest req) {
    return ResponseEntity.ok(authService.login(req));
  }

  @PostMapping("/refresh")
  public ResponseEntity<TokenResponse> refresh(@Valid @RequestBody RefreshRequest req) {
    return ResponseEntity.status(501).build(); // TODO impl
  }

  @PostMapping("/logout")
  public ResponseEntity<Void> logout(@Valid @RequestBody RefreshRequest req) {
    return ResponseEntity.status(501).build(); // TODO impl
  }

  @GetMapping("/exists")
  public ResponseEntity<Boolean> isMailIdExists(String mailId) {
    return ResponseEntity.ok(authService.isMailIdExists(mailId));
  }
}
