# syntax=docker/dockerfile:1

################################################################################

# Create a stage for resolving and downloading dependencies.
FROM maven:3.9-eclipse-temurin-17 AS deps

WORKDIR /build

# Copy the pom.xml file
COPY pom.xml .

# Download dependencies as a separate step to take advantage of Docker's caching.
# Leverage a cache mount to /root/.m2 so that subsequent builds don't have to
# re-download packages.
RUN --mount=type=cache,target=/root/.m2 \
    mvn dependency:go-offline -DskipTests -U

################################################################################

# Create a stage for building the application
FROM deps AS package

WORKDIR /build

COPY ./src src/
RUN --mount=type=cache,target=/root/.m2 \
    mvn package -DskipTests && \
    mv target/events-service-1.0-SNAPSHOT.jar target/app.jar

################################################################################

# Create a stage for extracting the application into separate layers.
FROM package AS extract

WORKDIR /build

RUN java -Djarmode=layertools -jar target/app.jar extract --destination target/extracted

################################################################################

# Create a new stage for running the application
FROM eclipse-temurin:17-jre-jammy AS final

# Create a non-privileged user for the app
ARG UID=10001
RUN adduser \
    --disabled-password \
    --gecos "" \
    --home "/nonexistent" \
    --shell "/sbin/nologin" \
    --no-create-home \
    --uid "${UID}" \
    appuser
USER appuser

# Copy the executable from the "extract" stage.
COPY --from=extract build/target/extracted/dependencies/ ./
COPY --from=extract build/target/extracted/spring-boot-loader/ ./
COPY --from=extract build/target/extracted/snapshot-dependencies/ ./
COPY --from=extract build/target/extracted/application/ ./

EXPOSE 4041

ENTRYPOINT [ "java", "org.springframework.boot.loader.launch.JarLauncher" ]
