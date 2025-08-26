package org.shathursan.service.impl;

import org.shathursan.entity.User;
import org.shathursan.service.UserService;
import org.springframework.stereotype.Service;

@Service
public class UserServiceImpl implements UserService {

  @Override
  public User getMe() {
    throw new UnsupportedOperationException("TODO");
  }

  @Override
  public void assignRole(Long userId, String role) {
    throw new UnsupportedOperationException("TODO");
  }

}
