package org.shathursan.service.impl;

import java.util.Map;
import java.util.Set;
import lombok.RequiredArgsConstructor;
import org.shathursan.dto.LoginRequest;
import org.shathursan.dto.RefreshRequest;
import org.shathursan.dto.RegisterRequest;
import org.shathursan.dto.response.TokenResponse;
import org.shathursan.entity.Role;
import org.shathursan.entity.User;
import org.shathursan.repository.UserRepository;
import org.shathursan.security.JwtService;
import org.shathursan.service.AuthService;
import org.shathursan.utils.Constants;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.BadCredentialsException;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;

@Service
@RequiredArgsConstructor
public class AuthServiceImpl implements AuthService {

  private final UserRepository userRepository;
  private final JwtService jwtService;
  private final PasswordEncoder passwordEncoder;
  private final AuthenticationManager authenticationManager;

  @Value("${jwt.access-token-ttl-min}")
  private long accessTtlMin;

  @Override
  public TokenResponse register(RegisterRequest req) {
    if (userRepository.existsByEmail(req.getEmail())) {
      throw new IllegalArgumentException("Email already exists");
    }

    var user = User.builder()
        .email(req.getEmail())
        .password(passwordEncoder.encode(req.getPassword()))
        .firstName(req.getFirstName())
        .lastName(req.getLastName())
        .enabled(true)
        .accountNonLocked(true)
        .roles(Set.of(Role.ORGANIZER))
        .build();
    userRepository.save(user);

    var roles = user.getRoles().stream().map(Enum::name).toList();
    var claims = Map.<String, Object>of(
        Constants.CLAIM_UID, user.getId().toString(),
        Constants.CLAIM_ROLES, roles
    );
    String access = jwtService.generateAccessToken(user.getEmail(), claims);

    return TokenResponse.builder()
        .status("success")
        .tokenType("Bearer")
        .accessToken(access)
        .refreshToken("")            // refresh not implemented in this skeleton
        .expiresInSec(accessTtlMin * 60)
        .build();

  }

  @Override
  public TokenResponse login(LoginRequest req) {
    try{
      authenticationManager.authenticate(
          new UsernamePasswordAuthenticationToken(req.getEmail(), req.getPassword())
      );
    }catch(BadCredentialsException ex){
      throw new IllegalArgumentException("Invalid email or password", ex);
    }

    var user = userRepository.findByEmail(req.getEmail())
        .orElseThrow(() -> new IllegalArgumentException("User not found"));

    var roles = user.getRoles().stream().map(Enum::name).toList();
    var claims = Map.<String, Object>of(
        Constants.CLAIM_UID, user.getId().toString(),
        Constants.CLAIM_ROLES, roles
    );
    String access = jwtService.generateAccessToken(user.getEmail(), claims);
    return TokenResponse.builder()
        .status("success")
        .tokenType("Bearer")
        .accessToken(access)
        .refreshToken("")            // refresh not implemented in this skeleton
        .expiresInSec(accessTtlMin * 60)
        .build();
  }

  @Override
  public TokenResponse refresh(RefreshRequest req) {
    throw new UnsupportedOperationException("TODO");
  }

  @Override
  public void logout(String refreshToken) {
    throw new UnsupportedOperationException("TODO");
  }

  @Override
  public Boolean isMailIdExists(String mailId) {
    // check if userId and MAilId exists
    boolean userEmail = userRepository.existsByEmail( mailId);
    return userEmail;
  }


}
