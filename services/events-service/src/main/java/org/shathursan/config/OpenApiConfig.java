package org.shathursan.config;


import io.swagger.v3.oas.models.Components;
import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.info.Info;
import io.swagger.v3.oas.models.security.SecurityRequirement;
import io.swagger.v3.oas.models.security.SecurityScheme;
import org.springdoc.core.models.GroupedOpenApi;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class OpenApiConfig {
  private static final String BEARER = "BearerAuth";

  @Bean
  public OpenAPI openAPI() {
    return new OpenAPI()
        .info(new Info().title("Event Service API").version("v1").description("Public browse + Organizer CRUD"))
        .components(new Components().addSecuritySchemes(BEARER,
            new SecurityScheme().name(BEARER).type(SecurityScheme.Type.HTTP).scheme("bearer").bearerFormat("JWT")))
        .addSecurityItem(new SecurityRequirement().addList(BEARER));
  }

  @Bean
  public GroupedOpenApi eventApi() {
    return GroupedOpenApi.builder().group("event").pathsToMatch("/api/**").build();
  }

}
