# Distributed Tracing Implementation

This document describes the comprehensive distributed tracing implementation added to the Ticketer microservices application.

## Overview

The implementation provides end-to-end observability across all microservices using OpenTelemetry with both C# and Java services, enhanced saga orchestration tracing, and custom business metrics.

## Architecture

### C# Services (.NET 8.0)
- **BookingService**: Enhanced saga orchestration with detailed state transition tracing
- **TicketService**: Consumer tracing for ticket hold/reservation operations
- **PaymentService**: Payment processing tracing with success/failure metrics
- **Gateway.Api**: API gateway with request/response tracing
- **SharedLibrary**: Common tracing utilities and telemetry infrastructure

### Java Services (Spring Boot 3.1.4, Java 17)
- **authentication-service**: User authentication with Micrometer/OpenTelemetry tracing
- **events-service**: Event management with distributed tracing
- **notification-service**: Email notification service with tracing

## Key Components

### SharedLibrary.Tracing Namespace

#### TicketerTelemetry
Centralized telemetry configuration with:
- **ActivitySources**: Domain-specific activity sources for each service
- **Meters**: Custom metrics for business and saga operations
- **Counters**: Booking creations, payments processed, tickets reserved, saga completions/failures
- **Histograms**: Saga execution duration tracking

#### TracingExtensions
Extension methods for enhanced tracing:
- `StartSagaActivity()`: Creates activities for saga state transitions
- `StartPublishActivity()`: Message publishing with trace context propagation
- `AddTraceContext()`: Adds trace context to message headers
- `RecordStateTransition()`: Records saga state changes
- `RecordSagaFailure()`: Handles saga failure scenarios

## Saga Orchestration Tracing

### BookingStateMachine Enhancements
The booking saga now includes comprehensive tracing:

```csharp
// State transition tracing
using var activity = context.StartSagaActivity("booking.created", context.Saga);
activity?.RecordStateTransition(context.Saga.CurrentState ?? "Initial", "BookingCreated");

// Business metrics recording
TicketerTelemetry.SagaStartedCounter.Add(1, new TagList
{
    {"saga.type", "BookingOrchestration"},
    {"booking.id", context.Saga.BookingId.ToString()},
    {"customer.id", context.Saga.CustomerId ?? "unknown"}
});
```

### Message Consumer Tracing

Enhanced consumers with detailed operation tracking:

#### HoldTicketsConsumer
- Tracks ticket hold operations
- Records success/failure metrics
- Traces ticket reservation attempts

#### ProcessPaymentConsumer
- Monitors payment processing
- Records payment method and results
- Tracks payment failures with reasons

## Configuration

### C# Services (Program.cs)
```csharp
.WithTracing(tracing => tracing
    .AddSource("MassTransit")
    .AddSource("Ticketer.Saga")
    .AddSource("Ticketer.Booking")
    // ... other sources
)
.WithMetrics(metrics => metrics
    .AddMeter("Ticketer.Saga.Metrics")
    .AddMeter("Ticketer.Business.Metrics")
    // ... other meters
)
```

### Java Services (application.properties)
```properties
# Distributed Tracing with Micrometer/OpenTelemetry
management.tracing.sampling.probability=1.0
management.otlp.tracing.endpoint=${OTLP_TRACING_ENDPOINT:http://otel-collector:4318/v1/traces}
```

## Monitoring Stack

The existing Tiltfile provides the monitoring infrastructure:
- **Jaeger UI**: http://localhost:16686 (trace visualization)
- **Prometheus**: http://localhost:9090 (metrics collection)
- **Grafana**: http://localhost:3000 (dashboards)
- **OTEL Collector**: 
  - gRPC endpoint: http://localhost:4317
  - HTTP endpoint: http://localhost:4318

## Trace Context Propagation

Traces are correlated across service boundaries using:
- **Correlation IDs**: Saga correlation IDs for workflow tracking
- **Trace Headers**: Automatic propagation through MassTransit messages
- **Activity Context**: Distributed trace context preservation
- **Business IDs**: Booking IDs, Event IDs, User IDs for business correlation

## Custom Metrics

### Saga Metrics
- `saga.started`: Number of sagas initiated
- `saga.completed`: Successfully completed sagas
- `saga.failed`: Failed sagas with failure reasons
- `saga.duration`: Saga execution time histogram

### Business Metrics  
- `bookings.created`: Total booking creations
- `payments.processed`: Payment transaction count
- `tickets.reserved`: Ticket reservation count

## Benefits

1. **End-to-End Visibility**: Complete request flow tracking across all services
2. **Saga Orchestration Monitoring**: Detailed state transition tracking for complex workflows
3. **Performance Monitoring**: Duration tracking for saga operations and service calls
4. **Error Tracking**: Comprehensive error recording with context
5. **Business Insights**: Custom metrics for business process monitoring
6. **Debugging Support**: Rich context for troubleshooting distributed issues

## Usage

### Viewing Traces
1. Navigate to Jaeger UI: http://localhost:16686
2. Select service (e.g., "BookingService")
3. Search by operation (e.g., "saga.booking.created")
4. Filter by tags (booking.id, saga.type, etc.)

### Monitoring Metrics
1. Access Prometheus: http://localhost:9090
2. Query custom metrics: `saga_started_total`, `bookings_created_total`
3. View in Grafana dashboards with visualization

### Development
Use the SharedLibrary.Tracing utilities in new services:
```csharp
using var activity = TicketerTelemetry.PaymentActivitySource.StartActivity("process.payment");
activity?.SetTag("payment.amount", amount);
// ... business logic
activity?.SetTag("payment.result", success ? "success" : "failed");
```

This implementation provides comprehensive observability for the Ticketer microservices platform, enabling effective monitoring, debugging, and performance optimization of the distributed system.