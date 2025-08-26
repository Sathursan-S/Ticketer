package org.shathursan.dto;

public class OrganizerDetails {

  private final String userId;
  private final String userEmail;

  public OrganizerDetails(String userId, String userEmail) {
    this.userId = userId;
    this.userEmail = userEmail;
  }

  public String getUserId() {
    return userId;
  }

  public String getUserEmail() {
    return userEmail;
  }
}
