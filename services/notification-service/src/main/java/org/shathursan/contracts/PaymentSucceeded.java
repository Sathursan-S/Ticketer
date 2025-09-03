package org.shathursan.contracts;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.Data;
import java.time.Instant;
import java.util.UUID;

@Data
public class PaymentSucceeded {
  @JsonProperty("BookingId")       private UUID bookingId;
  @JsonProperty("CustomerId")      private UUID customerId;
  @JsonProperty("PaymentIntentId") private String paymentIntentId;
  @JsonProperty("Amount")          private double amount;
  @JsonProperty("Currency")        private String currency;
  @JsonProperty("PaidAtUtc")       private Instant paidAtUtc;
}