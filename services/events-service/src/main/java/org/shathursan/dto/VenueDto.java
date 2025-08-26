package org.shathursan.dto;

import jakarta.validation.constraints.NotBlank;
import lombok.Data;
import lombok.RequiredArgsConstructor;
import org.shathursan.entity.VenueInfo;

@Data
public class VenueDto{

  @NotBlank
  private String name;
  private String addressLine1;
  private String addressLine2;
  private String city;
  private String country;

  public VenueInfo toEntity() {
    return VenueInfo.builder()
        .name(name)
        .addressLine1(addressLine1)
        .addressLine2(addressLine2)
        .city(city)
        .country(country)
        .build();
  }


}
