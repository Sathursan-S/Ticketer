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
-----------------------------------------------------------------
""".strip())

# Enable Kubernetes
allow_k8s_contexts('docker-desktop')  # Add your context here if different

# Load k8s resources from YAML files
k8s_yaml([
    'k8s/namespace.yaml',
    'k8s/configmaps.yaml',
    'k8s/gateway-config.yaml', 
    'k8s/storage.yaml',
    'k8s/services.yaml',
    'k8s/deployments.yaml',
    'k8s/ingress.yaml'
])

# Build images for .NET services with live update
# BookingService
docker_build(
    'bookingservice:1.0.0',
    context='.',
    dockerfile='./services/BookingService/Dockerfile',
    live_update=[
        sync('./services/BookingService', '/src/services/BookingService'),
        sync(SHARED_LIB_LOCAL, SHARED_LIB_CONTAINER)
    ]
)

# TicketService
docker_build(
    'ticketservice:1.0.0',
    context='.',
    dockerfile='./services/TicketService/Dockerfile',
    live_update=[
        sync('./services/TicketService', '/src/services/TicketService'),
        sync(SHARED_LIB_LOCAL, SHARED_LIB_CONTAINER)
    ]
)

# PaymentService
docker_build(
    'paymentservice:1.0.0',
    context='.',
    dockerfile='./services/PaymentService/Dockerfile',
    live_update=[
        sync('./services/PaymentService', '/src/services/PaymentService'),
        sync(SHARED_LIB_LOCAL, SHARED_LIB_CONTAINER)
    ]
)

# Gateway API
docker_build(
    'gatewayapi:1.0.0',
    context='.',
    dockerfile='./services/Gateway.Api/Dockerfile',
    live_update=[
        sync('./services/Gateway.Api', '/src/services/Gateway.Api'),
        sync(SHARED_LIB_LOCAL, SHARED_LIB_CONTAINER)
    ]
)

# Configure resource settings
# BookingService resource
k8s_resource(
    'booking-service',
    port_forwards=['8040:80'],
    resource_deps=['bookingservice-db', 'rabbitmq']
)

# TicketService resource
k8s_resource(
    'ticket-service',
    port_forwards=['8082:80'],
    resource_deps=['postgres-ticket', 'rabbitmq']
)

# PaymentService resource
k8s_resource(
    'payment-service',
    port_forwards=['8090:80'],
    resource_deps=['rabbitmq']
)

# Gateway API resource
k8s_resource(
    'gateway-api',
    port_forwards=['5000:80'],
    resource_deps=[
        'booking-service',
        'ticket-service', 
        'payment-service'
    ],
    labels=['api']
)

# RabbitMQ resource
k8s_resource(
    'rabbitmq',
    port_forwards=['5672:5672', '15672:15672'],
    labels=['infrastructure']
)

# Database resources
k8s_resource(
    'postgres-booking',
    port_forwards=['5436:5432'],
    labels=['infrastructure', 'database']
)

k8s_resource(
    'postgres-ticket',
    port_forwards=['5435:5432'],
    labels=['infrastructure', 'database']
)

# Local resources for helpful commands
local_resource(
    'k8s-dashboard',
    cmd='echo "Access Kubernetes Dashboard at http://localhost:8001/api/v1/namespaces/kubernetes-dashboard/services/https:kubernetes-dashboard:/proxy/"',
    auto_init=False
)

local_resource(
    'open-swagger',
    cmd='echo "Access API Gateway Swagger at http://localhost:5000/swagger/index.html"',
    auto_init=False
)

local_resource(
    'open-rabbitmq',
    cmd='echo "Access RabbitMQ Management UI at http://localhost:15672 (guest/guest)"',
    auto_init=False
)

# Display project information when Tilt starts
local_resource(
    'project-info',
    cmd='',
    auto_init=True,
    serve_cmd='echo "Ticketer Microservices project is running. Access the API Gateway at http://localhost:5000"'
)
