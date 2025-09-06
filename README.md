# 🎫 Ticketer - Event Ticketing Microservices Platform

A cloud-native event ticketing platform built with microservices architecture, featuring distributed tracing, monitoring, and automated deployment with Kubernetes and Tilt.

## 🏗️ Architecture Overview

Ticketer is a microservices-based application consisting of:

### Core Services
- **Gateway API** - Main entry point and API gateway
- **Authentication Service** - User authentication and authorization (Java/Spring Boot)
- **Events Service** - Event management and catalog (Java/Spring Boot)
- **Ticket Service** - Ticket inventory and management (.NET Core)
- **Booking Service** - Booking and reservation handling (.NET Core)
- **Payment Service** - Payment processing (.NET Core)
- **Notification Service** - Email/SMS notifications (Java/Spring Boot)

### Infrastructure Services
- **PostgreSQL** - Primary database for most services
- **Redis** - Caching layer for Ticket Service
- **RabbitMQ** - Message queue for inter-service communication
- **OpenTelemetry Collector** - Distributed tracing collection
- **Jaeger** - Distributed tracing visualization
- **Prometheus** - Metrics collection
- **Grafana** - Metrics visualization and dashboards

## 📋 Prerequisites

### Required Software
- **Docker Desktop** with Kubernetes enabled
- **Tilt** (v0.33.0 or later) - [Installation Guide](https://docs.tilt.dev/install.html)
- **kubectl** - Kubernetes command-line tool
- **Git** - Version control

### System Requirements
- **RAM**: 8GB minimum, 16GB recommended
- **CPU**: 4+ cores recommended
- **Disk**: 10GB free space
- **OS**: Windows 10/11, macOS, or Linux

### Docker Desktop Configuration
1. Enable Kubernetes in Docker Desktop settings
2. Allocate at least 6GB RAM to Docker
3. Set Kubernetes context to `docker-desktop`:
   ```bash
   kubectl config use-context docker-desktop
   ```

## 🚀 Quick Start

### 1. Clone the Repository
```bash
git clone <repository-url>
cd Ticketer
```

### 2. Start the Development Environment
```bash
# Start all services with Tilt
tilt up

# Or run in CI mode (non-interactive)
tilt ci
```

### 3. Access the Application
Once all services are running (check Tilt UI), you can access:

| Service | URL | Description |
|---------|-----|-------------|
| **Gateway API** | http://localhost:5266 | Main API endpoint |
| **Tilt UI** | http://localhost:10350 | Development dashboard |

## 🔍 Monitoring & Observability

| Tool | URL | Credentials | Purpose |
|------|-----|-------------|---------|
| **Jaeger UI** | http://localhost:16686 | None | Distributed tracing |
| **Prometheus** | http://localhost:9090 | None | Metrics collection |
| **Grafana** | http://localhost:3000 | admin/admin | Dashboards |
| **RabbitMQ Management** | http://localhost:15672 | guest/guest | Message queue |

## 🛠️ Development Guide

### Project Structure
```
Ticketer/
├── services/
│   ├── Gateway.Api/              # API Gateway (.NET Core)
│   ├── authentication-service/   # Authentication (Java/Spring Boot)
│   ├── events-service/          # Events management (Java/Spring Boot)
│   ├── TicketService/           # Ticket management (.NET Core)
│   ├── BookingService/          # Booking management (.NET Core)
│   ├── PaymentService/          # Payment processing (.NET Core)
│   ├── notification-service/    # Notifications (Java/Spring Boot)
│   └── SharedLibrary/           # Shared .NET libraries
├── k8s/                         # Kubernetes manifests
│   ├── services/               # Service deployments
│   ├── infra/                  # Infrastructure (RabbitMQ, databases)
│   ├── monitoring/             # Monitoring stack
│   └── secrets/                # Kubernetes secrets
├── Tiltfile                    # Tilt configuration
└── README.md                   # This file
```

### Service Endpoints

#### Gateway API (Port 5266)
- Base URL: `http://localhost:5266`
- Swagger UI: `http://localhost:5266/swagger`

#### Individual Services (Development)
| Service | Port | Health Check |
|---------|------|--------------|
| Authentication | 4040 | `http://localhost:4040/health` |
| Events | 4041 | `http://localhost:4041/health` |
| Notifications | 4042 | `http://localhost:4042/health` |
| Tickets | 5300 | `http://localhost:5300/health` |
| Bookings | 5200 | `http://localhost:5200/health` |
| Payments | 8090 | `http://localhost:8090/health` |

### Database Connections (Development)
| Database | Port | Service | Credentials |
|----------|------|---------|-------------|
| Booking DB | 5436 | PostgreSQL | postgres/postgres |
| Ticket DB | 5437 | PostgreSQL | postgres/postgres |
| Auth DB | 5438 | PostgreSQL | postgres/postgres |
| Events DB | 5439 | PostgreSQL | postgres/postgres |
| Notification DB | 5440 | PostgreSQL | postgres/postgres |
| Redis | 6379 | Redis | No auth |

## 🔧 Development Workflow

### Using Tilt for Development

1. **Start Development Environment**:
   ```bash
   tilt up
   ```

2. **View Tilt Dashboard**:
   - Open http://localhost:10350
   - Monitor service health and logs
   - View resource dependencies

3. **Live Reloading**:
   - Code changes trigger automatic rebuilds
   - Services restart automatically
   - View logs in Tilt UI

4. **Stop Environment**:
   ```bash
   tilt down
   ```

### Making Code Changes

#### .NET Services (C#)
- Make changes to `.cs` or `.csproj` files
- Tilt automatically triggers `dotnet build`
- Container updates with live reload

#### Java Services (Spring Boot)
- Make changes to `.java` or `pom.xml` files
- Tilt automatically triggers `mvn compile`
- Container updates with live reload

### Debugging Services

1. **View Service Logs**:
   ```bash
   kubectl logs -f deployment/<service-name>
   # Example: kubectl logs -f deployment/bookingservice
   ```

2. **Access Pod Shell**:
   ```bash
   kubectl exec -it deployment/<service-name> -- /bin/sh
   ```

3. **Port Forward for Direct Access**:
   ```bash
   kubectl port-forward service/<service-name> <local-port>:<service-port>
   ```

## 🏃‍♂️ Startup Sequence

The services start in a specific order to handle dependencies:

```
1. 🔐 Secrets & ConfigMaps
2. 🗄️  Databases (PostgreSQL instances)
3. 📨 Message Queue (RabbitMQ)
4. 📊 Monitoring Stack (Prometheus, Grafana, Jaeger, OTEL)
5. ⚡ Cache Layer (Redis)
6. 🔧 Health Checks (wait-for-* resources)
7. 🚀 Microservices (Authentication, Events, etc.)
8. 🌐 Gateway API (depends on all services)
```

## 📊 Observability Features

### Distributed Tracing
- **OpenTelemetry** integration across all services
- **Jaeger** for trace visualization
- Automatic trace correlation across service boundaries

### Metrics
- **Prometheus** metrics collection
- **Grafana** dashboards for visualization
- Service-level and infrastructure metrics

### Logging
- Structured logging in JSON format
- Centralized log aggregation
- Correlation IDs for request tracing

## 🧪 Testing

### Health Checks
All services expose health check endpoints:
```bash
# Check all services health
curl http://localhost:5266/health  # Gateway
curl http://localhost:4040/health  # Authentication
curl http://localhost:4041/health  # Events
curl http://localhost:4042/health  # Notifications
curl http://localhost:5300/health  # Tickets
curl http://localhost:5200/health  # Bookings
curl http://localhost:8090/health  # Payments
```

### Load Testing
```bash
# Example: Load test the gateway
curl -X GET http://localhost:5266/api/events
```

## 🐛 Troubleshooting

### Common Issues

#### Services Won't Start
1. **Check Docker Desktop**: Ensure Kubernetes is enabled and running
2. **Check Resources**: Ensure sufficient RAM (6GB+) allocated to Docker
3. **Check Context**: Verify kubectl context is set to `docker-desktop`
4. **View Logs**: Check Tilt UI for service logs and errors

#### Database Connection Issues
```bash
# Check if databases are running
kubectl get pods | grep db

# Check database logs
kubectl logs deployment/booking-db
```

#### Port Conflicts
```bash
# Check if ports are already in use
netstat -an | findstr :5266
netstat -an | findstr :16686
```

#### Kubernetes Issues
```bash
# Reset Kubernetes cluster
# In Docker Desktop: Settings → Kubernetes → Reset Kubernetes Cluster

# Check cluster status
kubectl cluster-info
kubectl get nodes
```

### Debugging Commands

```bash
# View all pods
kubectl get pods

# Describe a problematic pod
kubectl describe pod <pod-name>

# View service endpoints
kubectl get endpoints

# Check persistent volumes
kubectl get pv,pvc

# View secrets
kubectl get secrets
```

### Performance Issues
- Monitor resource usage in Tilt UI
- Check Grafana dashboards for bottlenecks
- Use Jaeger to identify slow traces
- Scale problematic services:
  ```bash
  kubectl scale deployment <service-name> --replicas=2
  ```

## 🚦 Environment Status

Use these commands to verify your environment:

```bash
# Check Tilt status
tilt get all

# Check all pods are running
kubectl get pods --all-namespaces

# Verify services are accessible
curl -s http://localhost:5266/health | jq .
curl -s http://localhost:16686/api/services | jq .
```

<<<<<<< HEAD
## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Make your changes and test locally with Tilt
4. Commit your changes: `git commit -m 'Add amazing feature'`
5. Push to the branch: `git push origin feature/amazing-feature`
6. Open a Pull Request
=======
>>>>>>> origin/copilot/vscode1757182545569

## 📚 Additional Resources

- [Tilt Documentation](https://docs.tilt.dev/)
- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [OpenTelemetry Documentation](https://opentelemetry.io/docs/)
- [Jaeger Documentation](https://www.jaegertracing.io/docs/)
- [Spring Boot Documentation](https://spring.io/projects/spring-boot)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)

<<<<<<< HEAD
## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.
=======
>>>>>>> origin/copilot/vscode1757182545569
