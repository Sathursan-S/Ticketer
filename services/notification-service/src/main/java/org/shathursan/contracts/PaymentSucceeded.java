package org.shathursan.contracts;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.Data;
import lombok.NoArgsConstructor;
import lombok.AllArgsConstructor;

import java.time.Instant;
import java.util.UUID;

@Data
@NoArgsConstructor
@AllArgsConstructor
public class PaymentSucceeded {
    @JsonProperty("BookingId")
    private UUID bookingId;
    @JsonProperty("PaymentIntentId")
    private String paymentIntentId;
    @JsonProperty("CustomerId")
    private String customerId;
    @JsonProperty("Amount")
    private double amount;
    @JsonProperty("PaymentMethod")
    private String paymentMethod;
}