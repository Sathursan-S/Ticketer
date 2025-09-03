package org.shathursan.contracts;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.Data;
import java.time.Instant;
import java.util.UUID;

@Data
public class PaymentFailed {
  @JsonProperty("BookingId")   private UUID bookingId;
  @JsonProperty("CustomerId")  private UUID customerId;
  @JsonProperty("Reason")      private String reason;
  @JsonProperty("FailedAtUtc") private Instant failedAtUtc;
}