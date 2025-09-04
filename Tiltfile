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

# Load Kubernetes manifests
k8s_yaml([
    'k8s/infra/rabbitmq/rabbitmq-deployment.yaml',
    'k8s/infra/rabbitmq/rabbitmq-service.yaml',
    'k8s/infra/rabbitmq/rabbitmq-pvc.yaml',
    'k8s/infra/rabbitmq/rabbitmq-configmap.yaml',
    'k8s/services/booking-service/deployment.yaml',
    'k8s/services/booking-service/service.yaml',
    'k8s/services/booking-service/booking-db-deployment.yaml',
    'k8s/services/booking-service/booking-db-service.yaml',
    'k8s/services/booking-service/booking-db-pvc.yaml',
])

# Build Docker images
docker_build('ticketer/booking-service', '.', dockerfile='./services/BookingService/Dockerfile')

# Define resources for Tilt UI
k8s_resource('bookingservice', port_forwards=['5200:80'])
k8s_resource('booking-db', port_forwards=['5436:5432'])
k8s_resource('rabbitmq', port_forwards=['15672:15672', '5672:5672'])

