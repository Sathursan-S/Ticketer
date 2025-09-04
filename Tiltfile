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
-----------------------------------------------------------------
""".strip())

# Enable Kubernetes
allow_k8s_contexts('docker-desktop')  # Add your context here if different

# Load Kubernetes manifests
k8s_yaml([
    'k8s/infra/rabbitmq/rabbitmq-deployment.yaml',
    'k8s/infra/rabbitmq/rabbitmq-service.yaml',
    'k8s/infra/rabbitmq/rabbitmq-pvc.yaml',
    'k8s/infra/rabbitmq/rabbitmq-configmap.yaml',
    'k8s/secrets/database-secrets.yaml',
    'k8s/secrets/rabbitmq-secrets.yaml',
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
    'k8s/services/booking-service/deployment.yaml',
    'k8s/services/booking-service/service.yaml',
    'k8s/services/booking-service/booking-db-deployment.yaml',
    'k8s/services/booking-service/booking-db-service.yaml',
    'k8s/services/booking-service/booking-db-pvc.yaml',
    'k8s/services/ticket-service/deployment.yaml',
    'k8s/services/ticket-service/service.yaml',
    'k8s/services/ticket-service/ticket-db-deployment.yaml',
    'k8s/services/ticket-service/ticket-db-service.yaml',
    'k8s/services/ticket-service/ticket-db-pvc.yaml',
    'k8s/services/ticket-service/redis-deployment.yaml',
    'k8s/services/ticket-service/redis-service.yaml',
    'k8s/services/ticket-service/redis-pvc.yaml',
    'k8s/services/ticket-service/ticket-service-configmap.yaml',
    'k8s/services/authentication-service/deployment.yaml',
    'k8s/services/authentication-service/service.yaml',
    'k8s/services/authentication-service/auth-db-deployment.yaml',
    'k8s/services/authentication-service/auth-db-service.yaml',
    'k8s/services/events-service/deployment.yaml',
    'k8s/services/events-service/service.yaml',
    'k8s/services/events-service/events-db-deployment.yaml',
    'k8s/services/events-service/events-db-service.yaml',
    'k8s/services/notification-service/deployment.yaml',
    'k8s/services/notification-service/service.yaml',
    'k8s/services/notification-service/notification-db-deployment.yaml',
    'k8s/services/notification-service/notification-db-service.yaml',
    'k8s/services/payment-service/deployment.yaml',
    'k8s/services/payment-service/service.yaml',
    'k8s/services/gateway-api/deployment.yaml',
    'k8s/services/gateway-api/service.yaml',
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
        run('dotnet build', trigger=['**/*.cs', '**/*.csproj']),
    ])
docker_build('ticketer/events-service', './services/events-service', dockerfile='./services/events-service/Dockerfile',
    live_update=[
        sync('./services/events-service/', '/app'),
        run('dotnet build', trigger=['**/*.cs', '**/*.csproj']),
    ])
docker_build('ticketer/notification-service', './services/notification-service', dockerfile='./services/notification-service/Dockerfile',
    live_update=[
        sync('./services/notification-service/', '/app'),
        run('dotnet build', trigger=['**/*.cs', '**/*.csproj']),
    ])
docker_build('ticketer/payment-service', '.', dockerfile='./services/PaymentService/Dockerfile',
    live_update=[
        sync('./services/PaymentService/', '/app'),
        run('dotnet build', trigger=['**/*.cs', '**/*.csproj']),
    ])
docker_build('ticketer/gateway-api', '.', dockerfile='./services/Gateway.Api/Dockerfile',
    live_update=[
        sync('./services/Gateway.Api/', '/app'),
        run('dotnet build', trigger=['**/*.cs', '**/*.csproj']),
    ])

# Define resources for Tilt UI
k8s_resource('bookingservice', port_forwards=['5200:80'], labels=["service"])
k8s_resource('booking-db', port_forwards=['5436:5432'], labels=["service"])
k8s_resource('ticketservice', port_forwards=['8080:5300'], labels=["service"])
k8s_resource('ticket-db', port_forwards=['5437:5432'], labels=["service"])
k8s_resource('redis', port_forwards=['6379:6379'], labels=["service"])
k8s_resource('authentication-service', port_forwards=['4040:4040'], labels=["service"])
k8s_resource('auth-db', port_forwards=['5438:5432'], labels=["service"])
k8s_resource('events-service', port_forwards=['4041:4041'], labels=["service"])
k8s_resource('events-db', port_forwards=['5439:5432'], labels=["service"])
k8s_resource('notification-service', port_forwards=['4042:4042'], labels=["service"])
k8s_resource('notification-db', port_forwards=['5440:5432'], labels=["service"])
k8s_resource('payment-service', port_forwards=['8090:8090'], labels=["service"])
k8s_resource('gateway-api', port_forwards=['5266:80'], labels=["service"])
k8s_resource('prometheus', 
    port_forwards=['9090:9090'], 
    labels=["monitoring"],
    links=[
        link("http://localhost:9090", "Prometheus UI"),
    ]
)
k8s_resource('grafana', 
    port_forwards=['3000:3000'], 
    labels=["monitoring"],
    links=[
        link("http://localhost:3000", "Grafana UI"),
    ]
)
k8s_resource('otel-collector', 
    port_forwards=['4317:4317', '4318:4318'], 
    labels=["monitoring"],
    links=[
        link("http://localhost:4317", "OTLP gRPC"),
        link("http://localhost:4318", "OTLP HTTP"),
    ]
)
k8s_resource('rabbitmq', port_forwards=['15672:15672', '5672:5672'])

# OpenTelemetry and Jaeger Distributed Tracing
#
# Services are configured with OpenTelemetry to send traces to the OTEL Collector,
# which forwards them to Jaeger for visualization.
# Traces can be viewed at http://localhost:16686
# OTEL Collector endpoints: http://localhost:4317 (gRPC) / http://localhost:4318 (HTTP)

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

