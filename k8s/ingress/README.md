# Ticketer NGINX Ingress API Gateway

This directory contains the NGINX Ingress Controller configuration for the Ticketer microservices API Gateway.

## Architecture

The API Gateway uses NGINX Ingress Controller to route traffic to the following microservices:

- **Authentication Service**: `/api/auth/*` → authentication-service:4040
- **Booking Service**: `/api/booking/*` → bookingservice:5200  
- **Ticket Service**: `/api/tickets/*` → ticketservice:8080
- **Events Service**: `/api/events/*` → events-service:4041
- **Notification Service**: `/api/notifications/*` → notification-service:4042
- **Payment Service**: `/api/payments/*` → payment-service:8090

## Components

### Core Components

1. **nginx-controller-rbac.yaml** - RBAC permissions for ingress controller
2. **nginx-controller-configmap.yaml** - Configuration settings for NGINX
3. **nginx-ingress-class.yaml** - Ingress class definition
4. **nginx-controller-service.yaml** - LoadBalancer service and default backend
5. **nginx-controller-deployment.yaml** - NGINX Ingress Controller deployment
6. **ticketer-ingress.yaml** - Main ingress routing rules

### Key Features

- **Path-based routing** with regex support
- **CORS enabled** for cross-origin requests
- **Health check endpoint** at `/health`
- **Default backend** for unmatched routes
- **URL rewriting** to strip API prefixes
- **Configurable timeouts** and body sizes

## Local Development

When running with Tilt, the ingress controller is accessible at:
- HTTP: `http://localhost:80`
- HTTPS: `http://localhost:443`

## API Endpoints

All microservice endpoints are accessible through the gateway:

```
http://localhost/api/auth/login          → authentication-service
http://localhost/api/booking/events      → booking-service
http://localhost/api/tickets/purchase    → ticket-service
http://localhost/api/events/list         → events-service
http://localhost/api/notifications/send  → notification-service
http://localhost/api/payments/process    → payment-service
```

## Health Monitoring

- Gateway health: `http://localhost/health`
- Individual service health checks are proxied through their respective paths

## Configuration

The NGINX configuration includes:
- 32MB request body size limit
- 600s proxy timeouts
- SSL/TLS v1.2+ support
- Real IP forwarding for proxies
- Custom error pages via default backend