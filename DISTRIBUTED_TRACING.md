# Distributed Tracing with Jaeger and OpenTelemetry

This document describes the distributed tracing implementation for the Ticketer microservices system using Jaeger and OpenTelemetry (OTLP).

## Overview

The system implements comprehensive distributed tracing across all microservices:

### C# Services (.NET 8.0)
- **Gateway.Api**: API Gateway with YARP reverse proxy
- **BookingService**: Booking management with MassTransit integration  
- **TicketService**: Ticket management with Redis caching
- **PaymentService**: Payment processing

### Java Services (Spring Boot 3.x)
- **authentication-service**: User authentication and JWT management
- **events-service**: Event management with RabbitMQ integration
- **notification-service**: Email notification service

## Architecture

```
Application Services
       ↓ (OTLP/Jaeger)
OpenTelemetry Collector  ←→  Jaeger All-in-One
       ↓ (Prometheus)
   Prometheus + Grafana
```

## Components

### 1. Jaeger
- **Purpose**: Primary tracing backend for trace storage and visualization
- **Ports**: 16686 (UI), 14250 (gRPC), 6831 (UDP agent)
- **Configuration**: All-in-one deployment with OTLP support enabled

### 2. OpenTelemetry Collector
- **Purpose**: Central telemetry data collection and processing
- **Ports**: 4317 (OTLP gRPC), 4318 (OTLP HTTP), 8889 (Prometheus metrics)
- **Features**: Batching, sampling, resource enrichment

### 3. Auto-Instrumentation

#### C# Services (.NET)
- **ASP.NET Core**: HTTP requests/responses
- **HttpClient**: Outbound HTTP calls
- **Entity Framework Core**: Database operations
- **Redis**: Cache operations (TicketService)
- **MassTransit**: Message queue operations

#### Java Services (Spring Boot)
- **Spring Web MVC**: HTTP requests/responses  
- **JDBC/Hibernate**: Database operations
- **Micrometer Tracing**: Spring Boot 3.x compatible tracing bridge

## Configuration

### Environment Variables

#### C# Services
```bash
OpenTelemetry__ServiceName=<ServiceName>
OpenTelemetry__Jaeger__Endpoint=jaeger:6831
OpenTelemetry__Otlp__Endpoint=http://otel-collector:4317
```

#### Java Services
```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
OTEL_SERVICE_NAME=<service-name>
spring.application.name=<service-name>
management.tracing.sampling.probability=1.0
```

### Service Configuration

#### C# Services
Each service configures OpenTelemetry in `Program.cs`:
```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "ServiceName"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddJaegerExporter()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddPrometheusExporter());
```

#### Java Services
Each service uses Micrometer tracing in `application.properties`:
```properties
management.tracing.enabled=true
management.tracing.sampling.probability=1.0
management.otlp.tracing.endpoint=http://otel-collector:4317
management.otlp.tracing.protocol=grpc
```

## Sampling Strategy

- **Development**: 100% sampling for full visibility
- **Production**: Configurable via `probabilistic_sampler` in OTLP Collector
- **Recommendation**: Start with 10-20% sampling in production

## Trace Attributes

### Standard Attributes
- `service.name`: Service identifier
- `service.version`: Service version
- `deployment.environment`: Environment (development/production)
- `http.method`, `http.url`, `http.status_code`: HTTP metadata
- `db.statement`, `db.connection_string`: Database operations

### Custom Business Attributes
- `user.id`: User identification in traces
- `booking.id`, `ticket.id`: Business entity IDs
- `payment.intent_id`: Payment processing correlation

## Deployment

### Docker Compose
```bash
# Start the full stack including tracing
docker-compose up -d

# Access Jaeger UI
http://localhost:16686

# Access Prometheus metrics
http://localhost:8889/metrics
```

### Kubernetes
```bash
# Apply monitoring manifests
kubectl apply -f k8s/monitoring/

# Port forward Jaeger UI
kubectl port-forward svc/jaeger-ui 16686:16686
```

## Troubleshooting

### Common Issues

1. **No traces appearing in Jaeger**
   - Check OTLP Collector connectivity: `curl http://otel-collector:4317`
   - Verify service configuration and environment variables
   - Check service logs for OpenTelemetry initialization

2. **Incomplete trace spans**
   - Verify auto-instrumentation is enabled for all dependencies
   - Check correlation ID propagation across service boundaries
   - Review custom instrumentation implementation

3. **Performance Impact**
   - Adjust sampling rate in production environments
   - Monitor collector resource usage and scaling
   - Consider async export and batching configurations

### Monitoring Commands

```bash
# Check OTLP Collector health
curl -s http://localhost:8889/metrics | grep otelcol_

# View Jaeger traces for specific service
curl -s "http://localhost:16686/api/traces?service=BookingService&limit=10"

# Check Prometheus metrics
curl -s http://localhost:8889/metrics | grep -E "(trace|span)_"
```

## Best Practices

1. **Correlation IDs**: Implement request correlation across all service boundaries
2. **Business Context**: Add meaningful business attributes to traces
3. **Error Handling**: Ensure error conditions are properly traced
4. **Performance**: Use appropriate sampling rates for production
5. **Security**: Avoid logging sensitive data in trace attributes
6. **Monitoring**: Set up alerts on trace error rates and latencies

## Extensions

### Future Enhancements
- [ ] Distributed context propagation with OpenTelemetry Baggage
- [ ] Custom trace exporters for additional backends
- [ ] Log correlation with trace IDs
- [ ] Trace-based alerting and SLO monitoring
- [ ] Service mesh integration (Istio/Linkerd)