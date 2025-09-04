# Ticketer Kubernetes Deployment

This directory contains Kubernetes manifests for deploying the Ticketer microservices platform.

## Architecture Overview

Ticketer is a microservices-based event ticketing platform with the following components:

### Core Services
- **Authentication Service** (Java) - User authentication and authorization
- **Booking Service** (.NET) - Event booking and reservation management  
- **Ticket Service** (.NET) - Ticket generation and validation
- **Events Service** (Java) - Event management and listing
- **Notification Service** (Java) - Email and notification delivery
- **Payment Service** (.NET) - Payment processing

### Infrastructure
- **NGINX Ingress API Gateway** - Routes external traffic to microservices
- **PostgreSQL Databases** - Data persistence for each service
- **Redis** - Caching and session storage
- **RabbitMQ** - Message queue for async communication
- **Monitoring Stack** - Prometheus, Grafana, Jaeger, OpenTelemetry

## Deployment Structure

```
k8s/
├── ingress/          # NGINX Ingress API Gateway
├── infra/            # Infrastructure components (RabbitMQ, etc.)
├── monitoring/       # Observability stack
├── secrets/          # Kubernetes secrets
└── services/         # Individual microservice deployments
```

## Quick Start

### Prerequisites
- Kubernetes cluster (Docker Desktop, Minikube, or cloud)
- kubectl configured
- Tilt installed (for development)

### Development Deployment
```bash
# Start all services with Tilt
tilt up

# Or deploy manually
./deploy-all.sh
```

### Accessing Services

All services are accessible through the NGINX Ingress API Gateway:

```
http://localhost/api/auth/*          → Authentication Service
http://localhost/api/booking/*       → Booking Service
http://localhost/api/tickets/*       → Ticket Service
http://localhost/api/events/*        → Events Service
http://localhost/api/notifications/* → Notification Service  
http://localhost/api/payments/*      → Payment Service
```

### Monitoring and Observability

- **Prometheus**: http://localhost:9090 - Metrics collection
- **Grafana**: http://localhost:3000 - Dashboards and visualization
- **Jaeger**: http://localhost:16686 - Distributed tracing
- **Gateway Health**: http://localhost/health - API gateway status

## Deployment Order

The services are deployed in the following order to respect dependencies:

1. **Secrets and Configuration** - Database credentials, RabbitMQ config
2. **Infrastructure** - RabbitMQ, Redis, databases
3. **Monitoring Stack** - Prometheus, Grafana, Jaeger, OTEL Collector
4. **Database Services** - PostgreSQL instances for each service
5. **Microservices** - Application services
6. **API Gateway** - NGINX Ingress Controller and routing rules

## Service Ports

| Service | Internal Port | External Port (via Tilt) |
|---------|---------------|---------------------------|
| Authentication | 4040 | 4040 |
| Booking | 5200 | 5200 |
| Ticket | 5300 (→8080) | 8080 |
| Events | 4041 | 4041 |
| Notification | 4042 | 4042 |
| Payment | 8090 | 8090 |
| **API Gateway** | **80/443** | **80/443** |

## Additional Resources

- [Ingress Gateway Documentation](./ingress/README.md)
- [Ingress Troubleshooting Guide](./ingress/TROUBLESHOOTING.md)
- [Tiltfile Configuration](../Tiltfile)
- [Docker Compose Alternative](../docker-compose.yml)
