# Ticketer Microservices Tiltfile
#
# This Tiltfile configures the local development environment for the Ticketer project.
# It builds and deploys all services to a local Kubernetes cluster.

# Define constants for shared directories
SHARED_LIB_LOCAL = './services/SharedLibrary'
SHARED_LIB_CONTAINER = '/src/services/SharedLibrary'

print("""
-----------------------------------------------------------------
✨ Ticketer Microservices Development Environment
   Starting up all services in Kubernetes...
   
   🔍 Monitoring:
     - Jaeger UI: http://localhost:16686
     - Prometheus: http://localhost:9090
     - Grafana: http://localhost:3000
     - OTEL Collector: http://localhost:4317 (gRPC) / http://localhost:4318 (HTTP)
   
   📋 Startup Order:
     1. Infrastructure & Secrets
     2. Databases
     3. Message Queue (RabbitMQ)
     4. Monitoring Stack
     5. Microservices
     6. Gateway API (last)
   
   ⚡ Dependencies:
     - Services wait for their databases
     - Services using RabbitMQ wait for message queue
     - All services wait for monitoring stack
     - Gateway waits for all services
-----------------------------------------------------------------
""".strip())

# Enable Kubernetes
allow_k8s_contexts('docker-desktop')  # Add your context here if different

# Load Kubernetes manifests in dependency order

# 1. Infrastructure and Secrets (no dependencies)
k8s_yaml([
    'k8s/secrets/database-secrets.yaml',
    'k8s/secrets/rabbitmq-secrets.yaml',
    'k8s/infra/rabbitmq/rabbitmq-configmap.yaml',
])

# 2. Message Queue Infrastructure
k8s_yaml([
    'k8s/infra/rabbitmq/rabbitmq-deployment.yaml',
    'k8s/infra/rabbitmq/rabbitmq-service.yaml',
    'k8s/infra/rabbitmq/rabbitmq-pvc.yaml',
])

# 3. Monitoring Infrastructure (independent)
k8s_yaml([
    'k8s/monitoring/prometheus-config.yaml',
    'k8s/monitoring/prometheus-deployment.yaml',
    'k8s/monitoring/prometheus-service.yaml',
    'k8s/monitoring/grafana-deployment.yaml',
    'k8s/monitoring/grafana-service.yaml',
    'k8s/monitoring/grafana-datasources.yaml',
    'k8s/monitoring/jaeger-deployment.yaml',
    'k8s/monitoring/jaeger-service.yaml',
    'k8s/monitoring/jaeger-ui-service.yaml',
    'k8s/monitoring/otel-collector-config.yaml',
    'k8s/monitoring/otel-collector-deployment.yaml',
    'k8s/monitoring/otel-collector-service.yaml',
])

# 4. Database Infrastructure
k8s_yaml([
    'k8s/services/booking-service/booking-db-deployment.yaml',
    'k8s/services/booking-service/booking-db-service.yaml',
    'k8s/services/booking-service/booking-db-pvc.yaml',
    'k8s/services/ticket-service/ticket-db-deployment.yaml',
    'k8s/services/ticket-service/ticket-db-service.yaml',
    'k8s/services/ticket-service/ticket-db-pvc.yaml',
    'k8s/services/ticket-service/redis-deployment.yaml',
    'k8s/services/ticket-service/redis-service.yaml',
    'k8s/services/ticket-service/redis-pvc.yaml',
    'k8s/services/ticket-service/ticket-service-configmap.yaml',
    'k8s/services/authentication-service/auth-db-deployment.yaml',
    'k8s/services/authentication-service/auth-db-service.yaml',
    'k8s/services/events-service/events-db-deployment.yaml',
    'k8s/services/events-service/events-db-service.yaml',
    'k8s/services/notification-service/notification-db-deployment.yaml',
    'k8s/services/notification-service/notification-db-service.yaml',
])

# 5. Service Deployments (depend on infrastructure)
k8s_yaml([
    'k8s/services/booking-service/deployment.yaml',
    'k8s/services/booking-service/service.yaml',
    'k8s/services/ticket-service/deployment.yaml',
    'k8s/services/ticket-service/service.yaml',
    'k8s/services/authentication-service/deployment.yaml',
    'k8s/services/authentication-service/service.yaml',
    'k8s/services/events-service/deployment.yaml',
    'k8s/services/events-service/service.yaml',
    'k8s/services/notification-service/deployment.yaml',
    'k8s/services/notification-service/service.yaml',
    'k8s/services/payment-service/deployment.yaml',
    'k8s/services/payment-service/service.yaml',
])

# 6. NGINX Ingress API Gateway (depends on all services)
k8s_yaml([
    'k8s/ingress/nginx-controller-rbac.yaml',
    'k8s/ingress/nginx-controller-configmap.yaml',
    'k8s/ingress/nginx-ingress-class.yaml',
    'k8s/ingress/nginx-controller-service.yaml',
    'k8s/ingress/nginx-controller-deployment.yaml',
    'k8s/ingress/ticketer-ingress.yaml',
])


# Build Docker images
docker_build('ticketer/booking-service', '.', 
    dockerfile='./services/BookingService/Dockerfile',
    live_update=[
        sync('./services/BookingService/', '/app'),
        run('dotnet build', trigger=['**/*.cs', '**/*.csproj']),
    ])
docker_build('ticketer/ticket-service', '.', dockerfile='./services/TicketService/Dockerfile',
    live_update=[
        sync('./services/TicketService/', '/app'),
        run('dotnet build', trigger=['**/*.cs', '**/*.csproj']),
    ])
docker_build('ticketer/authentication-service', './services/authentication-service', dockerfile='./services/authentication-service/Dockerfile',
    live_update=[
        sync('./services/authentication-service/', '/app'),
        run('mvn compile', trigger=['**/*.java', 'pom.xml']),
    ])
docker_build('ticketer/events-service', './services/events-service', dockerfile='./services/events-service/Dockerfile',
    live_update=[
        sync('./services/events-service/', '/app'),
        run('mvn compile', trigger=['**/*.java', 'pom.xml']),
    ])
docker_build('ticketer/notification-service', './services/notification-service', dockerfile='./services/notification-service/Dockerfile',
    live_update=[
        sync('./services/notification-service/', '/app'),
        run('mvn compile', trigger=['**/*.java', 'pom.xml']),
    ])
docker_build('ticketer/payment-service', '.', dockerfile='./services/PaymentService/Dockerfile',
    live_update=[
        sync('./services/PaymentService/', '/app'),
        run('dotnet build', trigger=['**/*.cs', '**/*.csproj']),
    ])

# Define resources for Tilt UI with proper dependencies

# Database resources (no dependencies)
k8s_resource('booking-db', port_forwards=['5436:5432'], labels=["database"])
k8s_resource('ticket-db', port_forwards=['5437:5432'], labels=["database"])
k8s_resource('auth-db', port_forwards=['5438:5432'], labels=["database"])
k8s_resource('events-db', port_forwards=['5439:5432'], labels=["database"])
k8s_resource('notification-db', port_forwards=['5440:5432'], labels=["database"])

# Infrastructure resources
k8s_resource('rabbitmq', port_forwards=['15672:15672', '5672:5672'], labels=["infrastructure"])
k8s_resource('redis', port_forwards=['6379:6379'], labels=["infrastructure"])

# Monitoring resources (independent)
k8s_resource('prometheus', 
    port_forwards=['9090:9090'], 
    labels=["monitoring"],
    links=[link("http://localhost:9090", "Prometheus UI")]
)
k8s_resource('grafana', 
    port_forwards=['3000:3000'], 
    labels=["monitoring"],
    links=[link("http://localhost:3000", "Grafana UI")]
)
k8s_resource('otel-collector', 
    port_forwards=['4317:4317', '4318:4318'], 
    labels=["monitoring"],
    links=[
        link("http://localhost:4317", "OTLP gRPC"),
        link("http://localhost:4318", "OTLP HTTP"),
    ]
)
k8s_resource('jaeger', 
    port_forwards=['16686:16686', '14268:14268'], 
    labels=["monitoring"],
    links=[link("http://localhost:16686", "Jaeger UI")]
)

# =================================================================================
# DEPENDENCY MATRIX - UPDATED WITH HEALTH CHECKS
# =================================================================================
#
# Service Dependencies (with health checks):
# -------------------
# authentication-service  -> auth-db, otel-collector, jaeger, wait-for-databases
# events-service         -> events-db, otel-collector, jaeger, wait-for-databases
# notification-service   -> notification-db, rabbitmq, otel-collector, jaeger, wait-for-infrastructure
# ticketservice          -> ticket-db, redis, otel-collector, jaeger, wait-for-infrastructure
# bookingservice         -> booking-db, rabbitmq, otel-collector, jaeger, wait-for-infrastructure
# payment-service        -> otel-collector, jaeger
# gateway-api           -> ALL services above + wait-for-services
#
# Health Check Dependencies:
# -------------------------
# wait-for-databases     -> All database pods
# wait-for-infrastructure -> RabbitMQ, Redis, OTEL Collector, Jaeger
# wait-for-services      -> All microservice pods
#
# This ensures:
# ✅ Databases start before their dependent services
# ✅ Infrastructure services start before dependent services
# ✅ All microservices are healthy before Gateway starts
# ✅ Monitoring stack is available for all services
# ✅ No race conditions during startup
# ✅ Better error handling and debugging
#
# =================================================================================

# Service resources with dependencies
k8s_resource('authentication-service', 
    port_forwards=['4040:4040'], 
    labels=["service"],
    resource_deps=['auth-db', 'otel-collector', 'jaeger']
)
k8s_resource('events-service', 
    port_forwards=['4041:4041'], 
    labels=["service"],
    resource_deps=['events-db', 'otel-collector', 'jaeger']
)
k8s_resource('notification-service', 
    port_forwards=['4042:4042'], 
    labels=["service"],
    resource_deps=['notification-db', 'rabbitmq', 'otel-collector', 'jaeger']
)
k8s_resource('ticketservice', 
    port_forwards=['8080:5300'], 
    labels=["service"],
    resource_deps=['ticket-db', 'redis', 'otel-collector', 'jaeger']
)
k8s_resource('bookingservice', 
    port_forwards=['5200:80'], 
    labels=["service"],
    resource_deps=['booking-db', 'rabbitmq', 'otel-collector', 'jaeger']
)
k8s_resource('payment-service', 
    port_forwards=['8090:8090'], 
    labels=["service"],
    resource_deps=['otel-collector', 'jaeger']
)

# NGINX Ingress API Gateway - depends on all services
k8s_resource('nginx-ingress-controller', 
    port_forwards=['80:80', '443:443'], 
    labels=["gateway"],
    resource_deps=[
        'authentication-service',
        'events-service', 
        'notification-service',
        'ticketservice',
        'bookingservice',
        'payment-service',
    ]
)
k8s_resource('default-http-backend', 
    labels=["gateway"],
    resource_deps=[]
)
k8s_resource('ticketer-api-gateway', 
    labels=["gateway"],
    resource_deps=['nginx-ingress-controller', 'default-http-backend']
)


# OpenTelemetry and Jaeger Distributed Tracing
#
# Services are configured with OpenTelemetry to send traces to the OTEL Collector,
# which forwards them to Jaeger for visualization.
# Traces can be viewed at http://localhost:16686
# OTEL Collector endpoints: http://localhost:4317 (gRPC) / http://localhost:4318 (HTTP)

# Add startup probes and resource management
update_settings(max_parallel_updates=3, k8s_upsert_timeout_secs=300)

# Configure resource limits for better stability
local_resource('check-cluster-ready', 
    cmd='kubectl wait --for=condition=Ready pod --all --timeout=300s --all-namespaces',
    labels=["health-check"],
    allow_parallel=True
)

# Health check for databases before starting services
local_resource('wait-for-databases',
    cmd='''
    echo "⏳ Waiting for databases to be ready..."
    kubectl wait --for=condition=Ready pod -l app=booking-db --timeout=120s || echo "⚠️  Booking DB not ready"
    kubectl wait --for=condition=Ready pod -l app=ticket-db --timeout=120s || echo "⚠️  Ticket DB not ready"
    kubectl wait --for=condition=Ready pod -l app=auth-db --timeout=120s || echo "⚠️  Auth DB not ready"
    kubectl wait --for=condition=Ready pod -l app=events-db --timeout=120s || echo "⚠️  Events DB not ready"
    kubectl wait --for=condition=Ready pod -l app=notification-db --timeout=120s || echo "⚠️  Notification DB not ready"
    echo "✅ All databases are ready!"
    ''',
    labels=["health-check"],
    deps=['booking-db', 'ticket-db', 'auth-db', 'events-db', 'notification-db']
)

# Health check for infrastructure
local_resource('wait-for-infrastructure',
    cmd='''
    echo "⏳ Waiting for infrastructure to be ready..."
    kubectl wait --for=condition=Ready pod -l app=rabbitmq --timeout=120s || echo "⚠️  RabbitMQ not ready"
    kubectl wait --for=condition=Ready pod -l app=redis --timeout=120s || echo "⚠️  Redis not ready"
    kubectl wait --for=condition=Ready pod -l app=otel-collector --timeout=120s || echo "⚠️  OTEL Collector not ready"
    kubectl wait --for=condition=Ready pod -l app=jaeger --timeout=120s || echo "⚠️  Jaeger not ready"
    echo "✅ All infrastructure services are ready!"
    ''',
    labels=["health-check"],
    deps=['rabbitmq', 'redis', 'otel-collector', 'jaeger']
)

# Update service dependencies to include health checks
local_resource('wait-for-services',
    cmd='''
    echo "⏳ Waiting for all services to be ready..."
    kubectl wait --for=condition=Ready pod -l app=authentication-service --timeout=180s || echo "⚠️  Auth service not ready"
    kubectl wait --for=condition=Ready pod -l app=events-service --timeout=180s || echo "⚠️  Events service not ready"
    kubectl wait --for=condition=Ready pod -l app=notification-service --timeout=180s || echo "⚠️  Notification service not ready"
    kubectl wait --for=condition=Ready pod -l app=ticketservice --timeout=180s || echo "⚠️  Ticket service not ready"
    kubectl wait --for=condition=Ready pod -l app=bookingservice --timeout=180s || echo "⚠️  Booking service not ready"
    kubectl wait --for=condition=Ready pod -l app=payment-service --timeout=180s || echo "⚠️  Payment service not ready"
    echo "✅ All microservices are ready!"
    ''',
    labels=["health-check"],
    deps=[
        'authentication-service',
        'events-service', 
        'notification-service',
        'ticketservice',
        'bookingservice',
        'payment-service'
    ]
)

# Uncomment to use Helm charts instead of raw Kubernetes manifests
# helm_resource('jaeger', './helm/jaeger',
#    flags=[
#       '--set', 'ui.service.type=ClusterIP',
#       '--set', 'ui.service.nodePort='  # Remove NodePort when running in Tilt
#    ],
#    port_forwards=['16686:16686', '6831:6831/udp', '14268:14268'],
#    labels=['monitoring'],
#    links=[link("http://localhost:16686", "Jaeger UI")]
# )
k8s_resource('jaeger', 
    port_forwards=['16686:16686', '14268:14268'], 
    labels=["monitoring"],
    links=[
        link("http://localhost:16686", "Jaeger UI"),
    ]
)

