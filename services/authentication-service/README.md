# Authentication Service

The Authentication Service is a Spring Boot-based microservice responsible for user authentication, authorization, and JWT token management in the Ticketer application.

## Overview

This service handles user registration, login, and JWT token generation/validation. It provides secure endpoints for user management and serves as the central authentication provider for the entire Ticketer ecosystem.

## Features

- **User Registration**: Register new users with email validation
- **User Authentication**: Secure login with JWT token generation
- **Token Management**: JWT token creation and validation
- **User Management**: User profile and account management
- **Security**: BCrypt password encryption and Spring Security integration
- **API Documentation**: Integrated Swagger/OpenAPI documentation
- **Health Monitoring**: Spring Boot Actuator with Prometheus metrics

## Technology Stack

- **Framework**: Spring Boot 3.1.4
- **Language**: Java 21
- **Database**: PostgreSQL
- **Security**: Spring Security + JWT (JJWT 0.11.5)
- **Documentation**: SpringDoc OpenAPI
- **Build Tool**: Maven
- **Monitoring**: Micrometer + Prometheus

## API Endpoints

### Authentication Endpoints (`/api/v1/auth`)

| Method | Endpoint | Description | Request Body | Response |
|--------|----------|-------------|--------------|----------|
| `POST` | `/register` | Register a new user | `RegisterRequest` | `TokenResponse` |
| `POST` | `/login` | Authenticate user | `LoginRequest` | `TokenResponse` |
| `POST` | `/refresh` | Refresh JWT token | `RefreshRequest` | `TokenResponse` (TODO) |
| `POST` | `/logout` | Logout user | `RefreshRequest` | `Void` (TODO) |
| `GET` | `/exists` | Check if email exists | Query param: `mailId` | `Boolean` |

### User Management Endpoints (`/api/v1/users`)

| Method | Endpoint | Description | Response |
|--------|----------|-------------|----------|
| `GET` | `/me` | Get current user profile | `UserResponse` |

## Data Models

### Request Models

**RegisterRequest**
```json
{
  "firstName": "string",
  "lastName": "string", 
  "email": "string",
  "password": "string"
}
```

**LoginRequest**
```json
{
  "email": "string",
  "password": "string"
}
```

**RefreshRequest**
```json
{
  "refreshToken": "string"
}
```

### Response Models

**TokenResponse**
```json
{
  "accessToken": "string",
  "refreshToken": "string",
  "tokenType": "Bearer",
  "expiresIn": "number"
}
```

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `SPRING_DATASOURCE_URL` | PostgreSQL connection URL | `jdbc:postgresql://localhost:5432/auth_db` |
| `SPRING_DATASOURCE_USERNAME` | Database username | `postgres` |
| `SPRING_DATASOURCE_PASSWORD` | Database password | `postgres` |
| `SPRING_JPA_HIBERNATE_DDL_AUTO` | Hibernate DDL mode | `update` |
| `JWT_SECRET` | JWT signing secret | Auto-generated |
| `JWT_EXPIRATION` | JWT expiration time (ms) | `86400000` (24 hours) |

### Application Ports

- **HTTP Port**: 4040
- **Database Port**: 5432 (external: 5432)

## Getting Started

### Prerequisites

- Java 21+
- Maven 3.6+
- PostgreSQL 15+
- Docker (optional)

### Local Development

1. **Clone the repository**
```bash
git clone <repository-url>
cd services/authentication-service
```

2. **Configure database**
```bash
# Start PostgreSQL (via Docker)
docker run --name auth-db \
  -e POSTGRES_DB=auth_db \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  -d postgres:15
```

3. **Run the application**
```bash
# Using Maven
mvn spring-boot:run

# Or build and run
mvn clean package
java -jar target/authentication-service-1.0-SNAPSHOT.jar
```

4. **Access the API**
- **API Base URL**: http://localhost:4040
- **Swagger UI**: http://localhost:4040/swagger-ui.html
- **OpenAPI Spec**: http://localhost:4040/v3/api-docs
- **Health Check**: http://localhost:4040/actuator/health
- **Metrics**: http://localhost:4040/actuator/prometheus

### Docker Deployment

1. **Build Docker image**
```bash
docker build -t ticketer/auth-service .
```

2. **Run with Docker Compose**
```bash
# From the service directory
docker compose up

# Or from the root directory
docker compose up auth-service
```

## Database Schema

The service uses JPA/Hibernate for database management with the following main entities:

### Users Table
- `id` (Primary Key)
- `first_name`
- `last_name` 
- `email` (Unique)
- `password` (Encrypted)
- `enabled`
- `created_at`
- `updated_at`

### Roles Table (if implemented)
- `id` (Primary Key)
- `name`
- `description`

## Security

- **Password Encryption**: BCrypt with configurable rounds
- **JWT Tokens**: RS256/HS256 signing with configurable expiration
- **CORS**: Configurable cross-origin resource sharing
- **Input Validation**: Jakarta Bean Validation
- **SQL Injection Protection**: JPA/Hibernate parameterized queries

## Monitoring and Health

### Health Checks
- **Database**: PostgreSQL connection status
- **Application**: Spring Boot health indicators
- **Custom**: Service-specific health checks

### Metrics
- **JVM**: Memory, GC, threads
- **HTTP**: Request counts, response times
- **Database**: Connection pool, query performance
- **Custom**: Authentication success/failure rates

## Testing

```bash
# Run unit tests
mvn test

# Run integration tests
mvn integration-test

# Run all tests with coverage
mvn clean verify
```

## API Examples

### Register a new user
```bash
curl -X POST http://localhost:4040/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    "password": "securePassword123"
  }'
```

### Login
```bash
curl -X POST http://localhost:4040/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com",
    "password": "securePassword123"
  }'
```

### Check email existence
```bash
curl -X GET "http://localhost:4040/api/v1/auth/exists?mailId=john.doe@example.com"
```

## Integration with Other Services

This service integrates with other Ticketer microservices:

- **Gateway API**: Routes authentication requests
- **All Services**: Validates JWT tokens for protected endpoints
- **Events Service**: User context for event creation
- **Booking Service**: User identification for bookings

## Development Notes

### TODO Items
- Implement refresh token functionality
- Implement logout functionality
- Add role-based authorization
- Add password reset functionality
- Add email verification
- Add rate limiting for auth endpoints

### Known Issues
- Refresh and logout endpoints return 501 (Not Implemented)
- No role-based access control implemented yet

## Contributing

1. Follow Java coding standards and Spring Boot best practices
2. Add unit tests for new functionality
3. Update API documentation
4. Ensure database migrations are backward compatible
5. Follow semantic versioning for releases

## Support

For issues and support:
- Check application logs: `docker logs auth-service`
- Monitor health endpoint: `/actuator/health`
- Review metrics: `/actuator/prometheus`
- Check Swagger documentation: `/swagger-ui.html`