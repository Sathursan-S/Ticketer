package org.shathursan.service;

import org.shathursan.dto.LoginRequest;
import org.shathursan.dto.RefreshRequest;
import org.shathursan.dto.RegisterRequest;
import org.shathursan.dto.response.TokenResponse;


public interface AuthService {

  TokenResponse register(RegisterRequest req);

  TokenResponse login(LoginRequest req);

  TokenResponse refresh(RefreshRequest req);

  void logout(String refreshToken);

  Boolean isMailIdExists(String mailId);
}
