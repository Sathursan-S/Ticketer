# Ticketer Architecture Documentation

## Table of Contents
1. [System Overview](#system-overview)
2. [Microservices Architecture](#microservices-architecture)
3. [Technology Stack](#technology-stack)
4. [Service Communication](#service-communication)
5. [Data Architecture](#data-architecture)
6. [Message Flow](#message-flow)
7. [Deployment Architecture](#deployment-architecture)
8. [Security Architecture](#security-architecture)
9. [Observability](#observability)
10. [Scalability and Performance](#scalability-and-performance)

## System Overview

Ticketer is a comprehensive event ticketing platform built using a microservices architecture. The system enables event organizers to create and manage events, while providing customers with a seamless ticket purchasing experience.

### Core Business Capabilities
- **Event Management**: Create, publish, and manage events
- **User Authentication**: Secure user registration and login
- **Ticket Management**: Create, reserve, and track tickets
- **Booking System**: Handle complex booking workflows with saga pattern
- **Payment Processing**: Secure payment transactions
- **Notifications**: Multi-channel communication system
- **API Gateway**: Unified access point for all services

### High-Level Architecture

```mermaid
graph TB
    subgraph "Client Layer"
        Web[Web Application]
        Mobile[Mobile App]
        API_Client[API Clients]
    end
    
    subgraph "API Gateway Layer"
        Gateway[Gateway API<br/>Port: 5000]
    end
    
    subgraph "Microservices Layer"
        Auth[Authentication Service<br/>Java/Spring Boot<br/>Port: 4040]
        Events[Events Service<br/>Java/Spring Boot<br/>Port: 4041]
        Notification[Notification Service<br/>Java/Spring Boot<br/>Port: 4042]
        Booking[Booking Service<br/>.NET/C#<br/>Port: 5200]
        Ticket[Ticket Service<br/>.NET/C#<br/>Port: 8082]
        Payment[Payment Service<br/>.NET/C#<br/>Port: 8090]
    end
    
    subgraph "Data Layer"
        AuthDB[(Auth DB<br/>PostgreSQL<br/>Port: 5432)]
        EventsDB[(Events DB<br/>PostgreSQL<br/>Port: 5433)]
        NotificationDB[(Notification DB<br/>PostgreSQL<br/>Port: 5437)]
        BookingDB[(Booking DB<br/>PostgreSQL<br/>Port: 5436)]
        TicketDB[(Ticket DB<br/>PostgreSQL<br/>Port: 5435)]
        Redis[(Redis<br/>Cache & Locks<br/>Port: 6379)]
    end
    
    subgraph "Infrastructure Layer"
        RabbitMQ[RabbitMQ<br/>Message Broker<br/>Port: 5672]
        Jaeger[Jaeger<br/>Tracing<br/>Port: 16686]
        Prometheus[Prometheus<br/>Metrics]
    end
    
    Web --> Gateway
    Mobile --> Gateway
    API_Client --> Gateway
    
    Gateway --> Auth
    Gateway --> Events
    Gateway --> Notification
    Gateway --> Booking
    Gateway --> Ticket
    Gateway --> Payment
    
    Auth --> AuthDB
    Events --> EventsDB
    Notification --> NotificationDB
    Booking --> BookingDB
    Ticket --> TicketDB
    Ticket --> Redis
    
    Auth -.->|JWT Validation| Events
    Auth -.->|JWT Validation| Booking
    
    Events -.->|RabbitMQ| RabbitMQ
    Booking -.->|RabbitMQ| RabbitMQ
    Ticket -.->|RabbitMQ| RabbitMQ
    Payment -.->|RabbitMQ| RabbitMQ
    Notification -.->|RabbitMQ| RabbitMQ
    
    RabbitMQ -.->|Events| Notification
    RabbitMQ -.->|Events| Ticket
    RabbitMQ -.->|Events| Booking
    
    Gateway --> Jaeger
    Auth --> Jaeger
    Events --> Jaeger
    Booking --> Jaeger
    Ticket --> Jaeger
    Payment --> Jaeger
    Notification --> Jaeger
```

## Microservices Architecture

### Service Boundaries

The system is decomposed into seven core microservices, each with clear responsibilities and data ownership:

#### 1. Authentication Service (Java/Spring Boot)
- **Port**: 4040
- **Database**: PostgreSQL (Port: 5432)
- **Responsibilities**:
  - User registration and authentication
  - JWT token generation and validation
  - User profile management
  - Role-based access control

#### 2. Events Service (Java/Spring Boot)
- **Port**: 4041
- **Database**: PostgreSQL (Port: 5433)
- **Responsibilities**:
  - Event lifecycle management (create, update, publish, cancel)
  - Event catalog and search
  - Event metadata management
  - Organizer-specific operations

#### 3. Notification Service (Java/Spring Boot)
- **Port**: 4042
- **Database**: PostgreSQL (Port: 5437)
- **Responsibilities**:
  - Multi-channel notification delivery (Email, SMS, Push)
  - Template management
  - Event-driven notification processing
  - Delivery tracking and retry logic

#### 4. Booking Service (.NET/C#)
- **Port**: 5200
- **Database**: PostgreSQL (Port: 5436) + MongoDB (for booking data)
- **Responsibilities**:
  - Booking workflow orchestration using Saga pattern
  - Distributed transaction coordination
  - Booking state management
  - Integration with Ticket and Payment services

#### 5. Ticket Service (.NET/C#)
- **Port**: 8082
- **Database**: PostgreSQL (Port: 5435)
- **Cache**: Redis (Port: 6379)
- **Responsibilities**:
  - Ticket inventory management
  - High-concurrency ticket operations with distributed locking
  - Ticket status lifecycle management
  - Bulk ticket operations

#### 6. Payment Service (.NET/C#)
- **Port**: 8090
- **Database**: Stateless (no persistent storage)
- **Responsibilities**:
  - Payment processing with external gateways
  - Payment method management
  - Transaction security and compliance
  - Saga participation for booking workflow

#### 7. Gateway API (.NET/C#)
- **Port**: 5000
- **Responsibilities**:
  - Reverse proxy and request routing
  - Service discovery and health monitoring
  - Unified API documentation
  - Cross-cutting concerns (CORS, rate limiting, observability)

## Technology Stack

### Programming Languages and Frameworks

```mermaid
graph LR
    subgraph "Java Ecosystem"
        Spring[Spring Boot 3.1.4]
        Maven[Maven Build Tool]
        JPA[JPA/Hibernate ORM]
    end
    
    subgraph ".NET Ecosystem"
        DotNet[.NET 9.0]
        EF[Entity Framework Core]
        MassTransit[MassTransit Messaging]
        YARP[YARP Reverse Proxy]
    end
    
    subgraph "Databases"
        PostgreSQL[PostgreSQL 15]
        MongoDB[MongoDB 6.0]
        Redis_Cache[Redis 7.0]
    end
    
    subgraph "Infrastructure"
        RabbitMQ_Infra[RabbitMQ 3.x]
        Docker_Infra[Docker & Compose]
        Kubernetes[Kubernetes]
    end
    
    subgraph "Observability"
        OpenTelemetry[OpenTelemetry]
        Jaeger_Obs[Jaeger Tracing]
        Prometheus_Obs[Prometheus Metrics]
    end
```

### Core Technologies by Category

#### **Backend Frameworks**
- **Java Services**: Spring Boot 3.1.4 with Java 21
- **.NET Services**: .NET 9.0 with C#
- **Build Tools**: Maven (Java), MSBuild (.NET)

#### **Databases**
- **Primary**: PostgreSQL 15 (one database per service)
- **Document Store**: MongoDB (Booking Service - booking data)
- **Cache/Locks**: Redis 7.0 (distributed locking, caching)

#### **Messaging**
- **Message Broker**: RabbitMQ 3.x
- **Java Integration**: Spring AMQP
- **.NET Integration**: MassTransit 8.5.2

#### **API Gateway**
- **Reverse Proxy**: YARP (Yet Another Reverse Proxy)
- **Documentation**: Unified Swagger/OpenAPI

#### **Observability**
- **Tracing**: OpenTelemetry + Jaeger
- **Metrics**: Prometheus + Micrometer
- **Logging**: Structured JSON logging

## Service Communication

### Communication Patterns

The system employs three primary communication patterns:

#### 1. Synchronous Communication (HTTP/REST)
Used for direct request-response interactions:

```mermaid
sequenceDiagram
    participant Client
    participant Gateway
    participant Service
    
    Client->>Gateway: HTTP Request
    Gateway->>Service: Forward Request
    Service-->>Gateway: HTTP Response
    Gateway-->>Client: Forward Response
```

**Use Cases**:
- Client-to-service communication via Gateway
- Service-to-service authentication validation
- Real-time data queries

#### 2. Asynchronous Messaging (RabbitMQ)
Used for event-driven communication and loose coupling:

```mermaid
sequenceDiagram
    participant Publisher
    participant RabbitMQ
    participant Consumer1
    participant Consumer2
    
    Publisher->>RabbitMQ: Publish Event
    RabbitMQ-->>Consumer1: Deliver Event
    RabbitMQ-->>Consumer2: Deliver Event
    Consumer1->>Consumer1: Process Event
    Consumer2->>Consumer2: Process Event
```

**Use Cases**:
- Event notifications (event created, updated, cancelled)
- Booking workflow coordination
- Notification delivery
- Ticket operations

#### 3. Saga Pattern (Distributed Transactions)
Used for managing complex business transactions across services:

```mermaid
sequenceDiagram
    participant Customer
    participant BookingService
    participant TicketService
    participant PaymentService
    participant NotificationService
    
    Customer->>BookingService: Create Booking
    BookingService->>BookingService: Start Saga
    BookingService->>TicketService: Hold Tickets
    
    alt Tickets Available
        TicketService-->>BookingService: Tickets Held
        BookingService->>PaymentService: Process Payment
        
        alt Payment Success
            PaymentService-->>BookingService: Payment Confirmed
            BookingService->>TicketService: Reserve Tickets
            BookingService->>NotificationService: Send Confirmation
            BookingService-->>Customer: Booking Confirmed
        else Payment Failed
            PaymentService-->>BookingService: Payment Failed
            BookingService->>TicketService: Release Tickets
            BookingService->>NotificationService: Send Failure Notice
            BookingService-->>Customer: Booking Failed
        end
    else Tickets Unavailable
        TicketService-->>BookingService: Hold Failed
        BookingService->>NotificationService: Send Unavailable Notice
        BookingService-->>Customer: Booking Failed
    end
```

### Message Routing and Queues

```mermaid
graph TB
    subgraph "RabbitMQ Message Broker"
        subgraph "Event Exchanges"
            EventExchange[Event Exchange]
            BookingExchange[Booking Exchange]
            PaymentExchange[Payment Exchange]
            TicketExchange[Ticket Exchange]
        end
        
        subgraph "Queues"
            EventQueue[event.notifications]
            BookingQueue[booking.notifications]
            PaymentQueue[payment.notifications]
            TicketQueue[ticket.operations]
        end
    end
    
    Events-->EventExchange
    Booking-->BookingExchange
    Payment-->PaymentExchange
    Ticket-->TicketExchange
    
    EventExchange-->EventQueue
    BookingExchange-->BookingQueue
    PaymentExchange-->PaymentQueue
    TicketExchange-->TicketQueue
    
    EventQueue-->Notification
    BookingQueue-->Notification
    PaymentQueue-->Notification
    TicketQueue-->Booking
```

## Data Architecture

### Database-Per-Service Pattern

Each microservice owns its data and database:

```mermaid
erDiagram
    users {
        uuid id PK
        string email UK
        string first_name
        string last_name
        string password_hash
        timestamp created_at
        timestamp updated_at
    }

    user_roles {
        uuid id PK
        uuid user_id FK
        string role_name
    }

    events {
        bigint id PK
        string event_name
        text description
        string category
        string location
        date event_date
        time start_time
        time end_time
        int ticket_capacity
        decimal ticket_price
        string organizer
        enum status
        timestamp created_at
        timestamp updated_at
    }

    tickets {
        uuid ticket_id PK
        bigint event_id FK
        enum status
        timestamp created_at
        timestamp updated_at
        int version
    }

    saga_state {
        uuid correlation_id PK
        string current_state
        uuid booking_id
        string customer_id
        bigint event_id
        int number_of_tickets
        json ticket_ids
        decimal payment_amount
    }

    outbox_messages {
        uuid id PK
        string message_type
        json payload
        timestamp created_at
        boolean processed
    }

    bookings {
        uuid booking_id PK
        string customer_id
        bigint event_id
        int number_of_tickets
        string status
        timestamp created_at
        timestamp updated_at
    }

    notifications {
        bigint id PK
        string recipient
        string subject
        text body
        string notification_type
        string status
        bigint event_id
        uuid booking_id
        timestamp created_at
        timestamp sent_at
        int retry_count
    }
    
    users ||--o{ user_roles : "has"
    events ||--o{ tickets : "has"
    events ||--o{ notifications : "triggers"
    saga_state ||--o{ outbox_messages : "generates"
    bookings ||--|| events : "is for"
    bookings ||--|| saga_state : "is linked to"
    bookings ||--|| notifications : "generates"
```

### Data Consistency Strategies

#### 1. Strong Consistency (Within Service)
- ACID transactions within each service's database
- Optimistic locking for concurrent operations (Ticket Service)
- Database constraints and foreign keys

#### 2. Eventual Consistency (Across Services)
- Saga pattern for distributed transactions
- Event sourcing for audit trails
- Compensating transactions for rollbacks

#### 3. Cache Strategy
- Redis for distributed locking (Ticket Service)
- Application-level caching for frequent queries
- Cache invalidation through messaging events

## Message Flow

### Event-Driven Architecture Flow

```mermaid
graph TD
    subgraph "Event Publishers"
        Events[Events Service]
        Booking[Booking Service]
        Payment[Payment Service]
        Ticket[Ticket Service]
    end
    
    subgraph "Message Broker"
        RMQ[RabbitMQ<br/>Event Bus]
    end
    
    subgraph "Event Consumers"
        Notification[Notification Service]
        BookingConsumer[Booking Saga]
        TicketConsumer[Ticket Service]
    end
    
    Events -->|EventCreated<br/>EventPublished<br/>EventCancelled| RMQ
    Booking -->|BookingCreated<br/>BookingConfirmed<br/>BookingFailed| RMQ
    Payment -->|PaymentSucceeded<br/>PaymentFailed| RMQ
    Ticket -->|TicketHeld<br/>TicketReserved<br/>TicketReleased| RMQ
    
    RMQ --> Notification
    RMQ --> BookingConsumer
    RMQ --> TicketConsumer
```

### Typical User Journey Flow

```mermaid
sequenceDiagram
    participant U as User
    participant G as Gateway
    participant A as Auth Service
    participant E as Events Service
    participant B as Booking Service
    participant T as Ticket Service
    participant P as Payment Service
    participant N as Notification Service
    
    U->>G: Register/Login
    G->>A: Authentication Request
    A-->>G: JWT Token
    G-->>U: Authentication Response
    
    U->>G: Browse Events
    G->>E: Get Events
    E-->>G: Events List
    G-->>U: Display Events
    
    U->>G: Create Booking
    G->>B: Create Booking (with JWT)
    B->>T: Hold Tickets
    T-->>B: Tickets Held
    B->>P: Process Payment
    P-->>B: Payment Confirmed
    B->>T: Reserve Tickets
    T-->>B: Tickets Reserved
    B->>N: Send Confirmation
    N->>U: Email Confirmation
    B-->>G: Booking Confirmed
    G-->>U: Success Response
```

## Deployment Architecture

### Docker Containerization

Each service is containerized for consistent deployment:

```mermaid
graph TB
    subgraph "Docker Containers"
        subgraph "Java Services"
            AuthContainer[auth-service:latest<br/>OpenJDK 21]
            EventsContainer[events-service:latest<br/>OpenJDK 21]
            NotificationContainer[notification-service:latest<br/>OpenJDK 21]
        end
        
        subgraph ".NET Services"
            BookingContainer[booking-service:latest<br/>.NET 9.0 Runtime]
            TicketContainer[ticket-service:latest<br/>.NET 9.0 Runtime]
            PaymentContainer[payment-service:latest<br/>.NET 9.0 Runtime]
            GatewayContainer[gateway-api:latest<br/>.NET 9.0 Runtime]
        end
        
        subgraph "Infrastructure"
            PostgreSQLContainer[postgres:15]
            MongoDBContainer[mongo:6.0]
            RedisContainer[redis:7-alpine]
            RabbitMQContainer[rabbitmq:3-management]
            JaegerContainer[jaegertracing/all-in-one]
        end
    end
    
    subgraph "Networks"
        TicketerNetwork[ticketer-network<br/>Bridge Network]
    end
    
    AuthContainer --- TicketerNetwork
    EventsContainer --- TicketerNetwork
    NotificationContainer --- TicketerNetwork
    BookingContainer --- TicketerNetwork
    TicketContainer --- TicketerNetwork
    PaymentContainer --- TicketerNetwork
    GatewayContainer --- TicketerNetwork
    PostgreSQLContainer --- TicketerNetwork
    RedisContainer --- TicketerNetwork
    RabbitMQContainer --- TicketerNetwork
    JaegerContainer --- TicketerNetwork
```

### Docker Compose Architecture

```yaml
# Simplified docker-compose.yml structure
version: '3.8'

services:
  # API Gateway
  apigateway:
    image: ticketer/gateway-api
    ports: ["5000:80"]
    depends_on: [bookingservice, ticketservice, paymentservice]
  
  # .NET Services
  bookingservice:
    image: ticketer/booking-service
    ports: ["5200:80"]
    depends_on: [bookingservice-db, rabbitmq]
  
  ticketservice:
    image: ticketer/ticket-service  
    ports: ["8082:80"]
    depends_on: [ticketservice-db, redis, rabbitmq]
  
  paymentservice:
    image: ticketer/payment-service
    ports: ["8090:8090"]
    depends_on: [rabbitmq]
  
  # Java Services
  auth-service:
    image: ticketer/auth-service
    ports: ["4040:4040"]
    depends_on: [auth-db]
  
  events-service:
    image: ticketer/events-service
    ports: ["4041:4041"]
    depends_on: [events-db, rabbitmq]
  
  notification-service:
    image: ticketer/notification-service
    ports: ["4042:4042"]
    depends_on: [notification-db, rabbitmq]

networks:
  ticketer-network:
    driver: bridge
```

### Kubernetes Deployment (Production)

```mermaid
graph TB
    subgraph "Kubernetes Cluster"
        subgraph "Ingress"
            IngressController[NGINX Ingress Controller]
        end
        
        subgraph "Services Layer"
            GatewayPod[Gateway API Pods<br/>Replicas: 2]
            AuthPod[Auth Service Pods<br/>Replicas: 2]
            EventsPod[Events Service Pods<br/>Replicas: 3]
            BookingPod[Booking Service Pods<br/>Replicas: 3]
            TicketPod[Ticket Service Pods<br/>Replicas: 5]
            PaymentPod[Payment Service Pods<br/>Replicas: 2]
            NotificationPod[Notification Service Pods<br/>Replicas: 2]
        end
        
        subgraph "Data Layer"
            PostgresPod[PostgreSQL StatefulSet<br/>Replicas: 3]
            RedisPod[Redis StatefulSet<br/>Replicas: 3]
            RabbitMQPod[RabbitMQ StatefulSet<br/>Replicas: 3]
        end
        
        subgraph "Monitoring"
            JaegerPod[Jaeger Deployment]
            PrometheusPod[Prometheus Deployment]
            GrafanaPod[Grafana Deployment]
        end
    end
    
    Internet --> IngressController
    IngressController --> GatewayPod
    GatewayPod --> AuthPod
    GatewayPod --> EventsPod
    GatewayPod --> BookingPod
    GatewayPod --> TicketPod
    GatewayPod --> PaymentPod
    GatewayPod --> NotificationPod
```

## Security Architecture

### Authentication and Authorization Flow

```mermaid
sequenceDiagram
    participant Client
    participant Gateway
    participant Auth
    participant Service
    
    Client->>Gateway: Login Request
    Gateway->>Auth: Validate Credentials
    Auth->>Auth: Generate JWT Token
    Auth-->>Gateway: JWT Token
    Gateway-->>Client: JWT Token
    
    Client->>Gateway: Service Request + JWT
    Gateway->>Gateway: Extract JWT
    Gateway->>Service: Forward Request + JWT
    Service->>Service: Validate JWT
    Service-->>Gateway: Response
    Gateway-->>Client: Response
```

### Security Layers

#### 1. API Gateway Security
- **CORS Policy**: Configurable cross-origin resource sharing
- **Rate Limiting**: Request throttling per client
- **IP Filtering**: Allow/deny specific IP ranges
- **HTTPS Enforcement**: Redirect HTTP to HTTPS

#### 2. Service-Level Security
- **JWT Validation**: Stateless token validation
- **Role-Based Access Control**: ORGANIZER, CUSTOMER roles
- **Input Validation**: Request payload validation
- **SQL Injection Protection**: Parameterized queries

#### 3. Data Security
- **Encryption at Rest**: Database encryption
- **Encryption in Transit**: TLS/HTTPS communication
- **Password Hashing**: BCrypt with salt
- **Secret Management**: Environment variable injection

#### 4. Infrastructure Security
- **Network Isolation**: Docker network segmentation
- **Database Access Control**: Service-specific database users
- **Message Queue Security**: RabbitMQ user authentication
- **Container Security**: Non-root user execution

### Security Configuration Example

```yaml
# Security configuration patterns
security:
  jwt:
    secret: ${JWT_SECRET}
    expiration: 86400000 # 24 hours
  
  cors:
    allowed-origins:
      - "https://ticketer.com"
      - "https://app.ticketer.com"
  
  rate-limiting:
    requests-per-minute: 60
    burst-capacity: 10
```

## Observability

### Three Pillars of Observability

#### 1. Distributed Tracing (Jaeger)
Tracks requests across service boundaries:

```mermaid
graph LR
    subgraph "Trace Span Hierarchy"
        Root[Gateway API<br/>Span ID: 1]
        Auth[Auth Service<br/>Span ID: 2<br/>Parent: 1]
        Booking[Booking Service<br/>Span ID: 3<br/>Parent: 1]
        Ticket[Ticket Service<br/>Span ID: 4<br/>Parent: 3]
        Payment[Payment Service<br/>Span ID: 5<br/>Parent: 3]
    end
    
    Root --> Auth
    Root --> Booking
    Booking --> Ticket
    Booking --> Payment
```

#### 2. Metrics Collection (Prometheus)
Key metrics across all services:

**Application Metrics**:
- Request rate (requests/second)
- Response time (latency percentiles)
- Error rate (4xx, 5xx responses)
- Active connections

**Business Metrics**:
- Booking conversion rate
- Payment success rate
- Ticket reservation success rate
- Event creation rate

**Infrastructure Metrics**:
- CPU and memory usage
- Database connection pools
- Message queue depth
- Cache hit/miss ratios

#### 3. Structured Logging
JSON-formatted logs with correlation IDs:

```json
{
  "timestamp": "2024-01-01T12:00:00Z",
  "level": "INFO",
  "service": "booking-service",
  "traceId": "abc123def456",
  "spanId": "789ghi012",
  "message": "Booking created successfully",
  "bookingId": "booking-123",
  "customerId": "customer-456",
  "eventId": 789
}
```

### Monitoring Dashboard Architecture

```mermaid
graph TB
    subgraph "Data Collection"
        Services[All Services<br/>OpenTelemetry]
        Infrastructure[Infrastructure<br/>Node Exporter]
    end
    
    subgraph "Storage"
        Prometheus[Prometheus<br/>Metrics Storage]
        Jaeger[Jaeger<br/>Trace Storage]
        Logs[Centralized Logs<br/>ELK Stack]
    end
    
    subgraph "Visualization"
        Grafana[Grafana<br/>Dashboards]
        JaegerUI[Jaeger UI<br/>Trace Analysis]
        Kibana[Kibana<br/>Log Analysis]
    end
    
    Services --> Prometheus
    Services --> Jaeger
    Services --> Logs
    Infrastructure --> Prometheus
    
    Prometheus --> Grafana
    Jaeger --> JaegerUI
    Logs --> Kibana
```

## Scalability and Performance

### Horizontal Scaling Strategy

```mermaid
graph TB
    subgraph "Load Balancer"
        LB[NGINX/HAProxy]
    end
    
    subgraph "Gateway Tier (Stateless)"
        Gateway1[Gateway API - 1]
        Gateway2[Gateway API - 2]
        GatewayN[Gateway API - N]
    end
    
    subgraph "Service Tier (Stateless)"
        Auth1[Auth Service - 1]
        Auth2[Auth Service - 2]
        Booking1[Booking Service - 1]
        Booking2[Booking Service - 2]
        Ticket1[Ticket Service - 1]
        Ticket2[Ticket Service - 2]
    end
    
    subgraph "Data Tier (Stateful)"
        PostgreSQLCluster[PostgreSQL Cluster<br/>Primary + Replicas]
        RedisCluster[Redis Cluster<br/>Master + Slaves]
        RabbitMQCluster[RabbitMQ Cluster<br/>3+ Nodes]
    end
    
    LB --> Gateway1
    LB --> Gateway2
    LB --> GatewayN
    
    Gateway1 --> Auth1
    Gateway1 --> Booking1
    Gateway1 --> Ticket1
    
    Auth1 --> PostgreSQLCluster
    Booking1 --> PostgreSQLCluster
    Ticket1 --> PostgreSQLCluster
    Ticket1 --> RedisCluster
```

### Performance Optimization Techniques

#### 1. Database Optimization
- **Connection Pooling**: Optimized pool sizes per service
- **Read Replicas**: Separate read/write workloads
- **Indexing Strategy**: Strategic database indexes
- **Query Optimization**: Efficient query patterns

#### 2. Caching Strategy
- **Application Cache**: In-memory caching for frequent queries
- **Distributed Cache**: Redis for shared cache across instances
- **HTTP Cache**: Gateway-level response caching
- **Database Query Cache**: PostgreSQL query result caching

#### 3. Concurrency Handling
- **Distributed Locking**: Redis-based locks for ticket operations
- **Optimistic Locking**: Version-based concurrency control
- **Async Processing**: Non-blocking I/O operations
- **Circuit Breaker**: Fail-fast for unhealthy services

#### 4. Message Queue Optimization
- **Message Batching**: Batch processing for notifications
- **Queue Partitioning**: Distribute load across queue partitions
- **Dead Letter Queues**: Handle failed message processing
- **Message Compression**: Reduce message size for performance

### Capacity Planning

#### Service Scaling Triggers
- **CPU Utilization**: > 70% average
- **Memory Usage**: > 80% of available
- **Response Time**: > 500ms P95 latency
- **Queue Depth**: > 1000 pending messages

#### Expected Load Characteristics
- **Peak Events**: 10,000 concurrent ticket purchases
- **Database QPS**: 5,000 queries per second per service
- **Message Throughput**: 100,000 messages per hour
- **API Gateway**: 50,000 requests per minute

This comprehensive architecture documentation provides a complete overview of the Ticketer system, from high-level design decisions to implementation details. The microservices architecture enables independent scaling, development, and deployment of each service while maintaining system cohesion through well-defined APIs and messaging patterns.
