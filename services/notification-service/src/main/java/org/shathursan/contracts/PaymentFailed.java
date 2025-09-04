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
public class PaymentFailed {
    @JsonProperty("BookingId")
    public UUID bookingId;
    @JsonProperty("CustomerId")
    public String customerId;
    @JsonProperty("Reason")
    public String reason;
    @JsonProperty("FailedAtUtc")
    public Instant failedAtUtc;
}