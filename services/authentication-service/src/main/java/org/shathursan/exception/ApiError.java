package org.shathursan.exception;

import java.time.Instant;
import java.util.List;
import lombok.Builder;
import lombok.Data;

@Data
@Builder
public class ApiError {
  private Instant timestamp;
  private int status;
  private String error;
  private String message;
  private List<String> details;
  private String path;
}
