package org.shathursan.service;

import java.util.UUID;
import org.shathursan.entity.User;

public interface UserService {

  User getMe();
  void assignRole(Long userId, String role);

}
