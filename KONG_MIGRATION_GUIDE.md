# API Gateway Migration: From .NET Gateway.Api to Kong

This document describes the complete migration from the custom .NET Gateway.Api service to Kong API Gateway with Kubernetes Ingress.

## Overview of Changes

### What Was Removed
- **Gateway.Api C# service** - Custom .NET reverse proxy using YARP
- **Gateway.Api Docker image** and related build configurations
- **Gateway.Api Kubernetes deployment** and service manifests
- **Direct routing configuration** in YARP format

### What Was Added
- **Kong API Gateway** - Industry-standard API gateway
- **PostgreSQL database** - Backend storage for Kong configuration
- **Kubernetes Ingress** - Cloud-native routing configuration
- **JWT authentication** - Token-based security for protected endpoints
- **Rate limiting & CORS** - Enhanced API protection and cross-origin support

## API Endpoint Mapping

The new Kong-based setup exposes the same API endpoints with enhanced security:

### Public Endpoints (No Authentication Required)
| Endpoint | Service | Port | Description |
|----------|---------|------|-------------|
| `POST /api/auth/register` | authentication-service | 4040 | User registration |
| `POST /api/auth/login` | authentication-service | 4040 | User login |
| `GET /api/auth/exists` | authentication-service | 4040 | Check if email exists |
| `GET /api/tickets/*` | ticketservice | 8080 | Get available tickets |

### Protected Endpoints (JWT Authentication Required)
| Endpoint | Service | Port | Description |
|----------|---------|------|-------------|
| `POST /api/events` | events-service | 4041 | Create new event |
| `GET /api/events` | events-service | 4041 | List events |
| `PUT /api/events/{id}` | events-service | 4041 | Update event |
| `DELETE /api/events/{id}` | events-service | 4041 | Cancel event |
| `POST /api/booking` | bookingservice | 80 | Create booking |
| `GET /api/booking/{id}` | bookingservice | 80 | Get booking details |
| `GET /api/booking/{id}/status` | bookingservice | 80 | Check booking status |

## Architecture Components

### Kong Gateway (`kong` namespace)
- **kong**: Main API gateway (2 replicas for HA)
- **kong-postgres**: PostgreSQL database for configuration storage
- **kong-migration**: One-time database setup job

### Kong Plugins
- **jwt-auth**: JWT token validation for protected endpoints
- **cors-plugin**: Cross-Origin Resource Sharing support
- **rate-limiting**: API rate limiting (100/min, 1000/hour per client)

### Ingress Configuration
- **ticketer-public-api**: Routes for public endpoints (auth, tickets)
- **ticketer-protected-api**: Routes for protected endpoints (events, booking)

## Deployment Guide

### Prerequisites
- Kubernetes cluster (Docker Desktop, minikube, or cloud provider)
- kubectl configured and connected to cluster
- Docker for building images (when using Tilt)

### Option 1: Complete Deployment (Recommended)
```bash
# Deploy all services including Kong
./k8s/deploy-all.sh

# Wait for Kong to be ready
kubectl wait --for=condition=available --timeout=300s deployment/kong -n kong

# Set up port forwarding
kubectl port-forward svc/kong-proxy 8000:80 -n kong &
kubectl port-forward svc/kong-admin 8001:8001 -n kong &
```

### Option 2: Development with Tilt
```bash
# Start development environment
tilt up

# Kong will be available at:
# - Proxy: http://localhost:8000
# - Admin: http://localhost:8001
```

### Option 3: Kong Only Deployment
```bash
# Deploy just Kong components
kubectl apply -f k8s/api-gateway/kong/kong-namespace.yaml
kubectl apply -f k8s/api-gateway/kong/kong-postgres.yaml
kubectl wait --for=condition=available --timeout=300s deployment/kong-postgres -n kong
kubectl apply -f k8s/api-gateway/kong/kong-migration.yaml
kubectl wait --for=condition=complete --timeout=300s job/kong-migration -n kong
kubectl apply -f k8s/api-gateway/kong/kong-deployment.yaml
kubectl apply -f k8s/api-gateway/kong-plugins.yaml
kubectl apply -f k8s/api-gateway/ingress.yaml
```

## Testing the API Gateway

### 1. Health Checks
```bash
# Kong proxy health
curl http://localhost:8000/

# Kong admin API status
curl http://localhost:8001/status
```

### 2. Public Endpoints (No Authentication)
```bash
# Check if email exists
curl "http://localhost:8000/api/auth/exists?mailId=test@example.com"

# User registration
curl -X POST http://localhost:8000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "newuser@example.com",
    "password": "password123",
    "firstName": "John",
    "lastName": "Doe"
  }'

# User login
curl -X POST http://localhost:8000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "newuser@example.com",
    "password": "password123"
  }'

# Get available tickets
curl http://localhost:8000/api/tickets
```

### 3. Protected Endpoints (JWT Required)
```bash
# First, get JWT token from login
TOKEN=$(curl -s -X POST http://localhost:8000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "user@example.com", "password": "password"}' \
  | jq -r '.accessToken')

# Create event (requires ORGANIZER role)
curl -X POST http://localhost:8000/api/events \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "eventName": "Sample Event",
    "description": "A test event",
    "eventDate": "2024-12-31T20:00:00Z",
    "ticketCapacity": 100,
    "ticketPrice": 25.00
  }'

# Create booking
curl -X POST http://localhost:8000/api/booking \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "eventId": 1,
    "customerId": "123e4567-e89b-12d3-a456-426614174000",
    "numberOfTickets": 2
  }'
```

## Configuration Management

### Kong Admin API
Kong provides a comprehensive admin API for runtime configuration:

```bash
# List all routes
curl http://localhost:8001/routes

# List all services
curl http://localhost:8001/services

# List all plugins
curl http://localhost:8001/plugins

# View Kong configuration
curl http://localhost:8001/status
```

### Plugin Management
Kong plugins can be managed via Kubernetes CRDs or Admin API:

```bash
# Add rate limiting to a specific route
curl -X POST http://localhost:8001/routes/{route-id}/plugins \
  -d "name=rate-limiting" \
  -d "config.minute=50"
```

## Monitoring and Troubleshooting

### Check Status
```bash
# Check all Kong-related resources
./k8s/status-check.sh

# Check Kong logs
kubectl logs -f deployment/kong -n kong

# Check PostgreSQL logs
kubectl logs -f deployment/kong-postgres -n kong
```

### Common Issues

1. **Kong not starting**
   - Check if PostgreSQL is running: `kubectl get pods -n kong`
   - Verify migration completed: `kubectl logs job/kong-migration -n kong`

2. **JWT authentication failing**
   - Verify JWT token format and expiration
   - Check plugin configuration: `kubectl get kongplugin jwt-auth -o yaml`

3. **Service not reachable**
   - Check if target services are running in default namespace
   - Verify service names match Ingress backend configuration
   - Check service ports match the target service definitions

## Security Considerations

### JWT Authentication
- JWT tokens are validated by Kong's jwt-auth plugin
- Tokens must include proper claims (exp, iss, roles)
- Secret key management is handled by the authentication service

### CORS Policy
- Configured for common development origins (localhost:3000, localhost:5173)
- Allows credentials and common HTTP methods
- Can be customized in `k8s/api-gateway/kong-plugins.yaml`

### Rate Limiting
- Default: 100 requests/minute, 1000 requests/hour per client
- Applied to all endpoints
- Can be customized per route or service

## Migration Benefits

1. **Industry Standard**: Kong is a battle-tested API gateway used by many enterprises
2. **Enhanced Security**: Built-in JWT validation, rate limiting, and CORS support
3. **Scalability**: Cloud-native architecture with PostgreSQL backend
4. **Flexibility**: Plugin ecosystem for additional features
5. **Monitoring**: Built-in metrics and admin API for observability
6. **Kubernetes Native**: Uses standard Ingress resources and CRDs

## Next Steps

1. **Custom Plugins**: Develop Kong plugins for specific business logic
2. **SSL/TLS**: Configure HTTPS certificates for production
3. **Monitoring**: Integrate with Prometheus/Grafana for metrics
4. **Backup**: Set up PostgreSQL backup strategy
5. **High Availability**: Configure multi-zone Kong deployment
6. **API Documentation**: Integrate with Kong's OpenAPI documentation features