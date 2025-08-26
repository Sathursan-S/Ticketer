package org.shathursan.exception;

import jakarta.servlet.http.HttpServletRequest;
import java.time.Instant;
import java.util.stream.Collectors;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;

@ControllerAdvice
public class GlobalExceptionHandler {

  @ExceptionHandler(MethodArgumentNotValidException.class)
  public ResponseEntity<ApiError> handleValidation(MethodArgumentNotValidException ex,
      HttpServletRequest req) {
    var details = ex.getBindingResult().getFieldErrors().stream()
        .map(f -> f.getField() + ": " + f.getDefaultMessage()).collect(
            Collectors.toList());
    var body = ApiError.builder().timestamp(Instant.now()).status(400).error("Bad Request")
        .message("Validation failed").details(details).path(req.getRequestURI()).build();
    return ResponseEntity.badRequest().body(body);
  }

  @ExceptionHandler({IllegalArgumentException.class, SecurityException.class})
  public ResponseEntity<ApiError> handleBadReq(RuntimeException ex, HttpServletRequest req) {
    var body = ApiError.builder().timestamp(Instant.now()).status(400).error("Bad Request")
        .message(ex.getMessage()).path(req.getRequestURI()).build();
    return ResponseEntity.badRequest().body(body);
  }
}
