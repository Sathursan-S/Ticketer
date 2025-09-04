# Kong API Gateway Setup

This directory contains the Kong API Gateway configuration for the Ticketer application.

## Overview

Kong replaces the previous .NET Gateway.Api service and provides:

- **API Gateway functionality** with advanced routing
- **JWT Authentication** for protected endpoints  
- **Rate limiting** and **CORS** support
- **PostgreSQL-backed configuration** for persistence
- **Admin API** for runtime configuration

## API Endpoints

### Public Endpoints (No Authentication)
- **Auth Service**: `GET|POST /api/auth/*` → `authentication-service:4040`
- **Available Tickets**: `GET /api/tickets/*` → `ticketservice:8080`

### Protected Endpoints (JWT Authentication Required)
- **Events CRUD**: `GET|POST|PUT|DELETE /api/events/*` → `events-service:4041`
- **Booking Operations**: `GET|POST|PUT /api/booking/*` → `bookingservice:80`

## Access URLs

- **Kong Proxy**: http://localhost:8000
- **Kong Admin**: http://localhost:8001  
- **Kong Status**: http://localhost:8001/status

## Authentication

Protected endpoints require a valid JWT token in the `Authorization` header:

```bash
Authorization: Bearer <jwt-token>
```

JWT tokens are obtained from the auth service:

```bash
# Register/Login
curl -X POST http://localhost:8000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password"}'
```

## Components

- **kong-namespace.yaml**: Kong namespace
- **kong-postgres.yaml**: PostgreSQL database for Kong
- **kong-migration.yaml**: Database migration job
- **kong-deployment.yaml**: Kong gateway deployment and services
- **kong-plugins.yaml**: Plugin configurations (JWT, CORS, rate limiting)
- **ingress.yaml**: Kubernetes Ingress routing rules

## Development

Use `tilt up` or deploy with:

```bash
./k8s/deploy-all.sh
```

The Kong admin API is available at http://localhost:8001 for configuration management.