#!/bin/bash

# Complete Ticketer Microservices Kubernetes Deployment Script
# This script deploys all services and infrastructure to Kubernetes

echo "🚀 Deploying Complete Ticketer Microservices to Kubernetes..."

# Create namespace if it doesn't exist
kubectl create namespace ticketer --dry-run=client -o yaml | kubectl apply -f -

# Deploy infrastructure first
echo "🏗️  Deploying Infrastructure..."
kubectl apply -f k8s/infra/rabbitmq/

# Deploy monitoring tools
echo "📊 Deploying Monitoring Tools..."
kubectl apply -f k8s/monitoring/

# Deploy databases and services
echo "🗄️  Deploying Databases and Services..."

# Booking Service
echo "📅 Deploying Booking Service..."
kubectl apply -f k8s/services/booking-service/

# Ticket Service
echo "🎫 Deploying Ticket Service..."
kubectl apply -f k8s/services/ticket-service/

# Authentication Service
echo "🔐 Deploying Authentication Service..."
kubectl apply -f k8s/services/authentication-service/

# Events Service
echo "📅 Deploying Events Service..."
kubectl apply -f k8s/services/events-service/

# Notification Service
echo "📧 Deploying Notification Service..."
kubectl apply -f k8s/services/notification-service/

# Payment Service
echo "💳 Deploying Payment Service..."
kubectl apply -f k8s/services/payment-service/

# Kong API Gateway
echo "🌐 Deploying Kong API Gateway..."
kubectl apply -f k8s/api-gateway/kong/kong-namespace.yaml
kubectl apply -f k8s/api-gateway/kong/kong-postgres.yaml
echo "⏳ Waiting for PostgreSQL to be ready..."
kubectl wait --for=condition=available --timeout=300s deployment/kong-postgres -n kong
kubectl apply -f k8s/api-gateway/kong/kong-migration.yaml
kubectl wait --for=condition=complete --timeout=300s job/kong-migration -n kong
kubectl apply -f k8s/api-gateway/kong/kong-deployment.yaml
kubectl apply -f k8s/api-gateway/kong/kong-ingress-controller.yaml
kubectl apply -f k8s/api-gateway/kong-plugins.yaml
kubectl apply -f k8s/api-gateway/ingress.yaml

# Wait for all deployments to be ready
echo "⏳ Waiting for all services to be ready..."
kubectl wait --for=condition=available --timeout=600s deployment/bookingservice
kubectl wait --for=condition=available --timeout=600s deployment/ticketservice
kubectl wait --for=condition=available --timeout=600s deployment/authentication-service
kubectl wait --for=condition=available --timeout=600s deployment/events-service
kubectl wait --for=condition=available --timeout=600s deployment/notification-service
kubectl wait --for=condition=available --timeout=600s deployment/payment-service
kubectl wait --for=condition=available --timeout=600s deployment/kong -n kong
kubectl wait --for=condition=available --timeout=600s deployment/kong-ingress-controller -n kong

echo "✅ All services deployed successfully!"
echo ""
echo "📊 Service Status:"
kubectl get pods
echo ""
echo "🔗 Access URLs:"
echo "  Kong API Gateway: http://localhost:8000"
echo "  Kong Admin API: http://localhost:8001"
echo "  API Endpoints:"
echo "    - Auth: http://localhost:8000/api/auth"
echo "    - Events: http://localhost:8000/api/events"
echo "    - Tickets: http://localhost:8000/api/tickets"
echo "    - Booking: http://localhost:8000/api/booking"
echo "  Booking Service (direct): http://localhost:5200"
echo "  Ticket Service (direct): http://localhost:8080"
echo "  Authentication Service (direct): http://localhost:4040"
echo "  Events Service (direct): http://localhost:4041"
echo "  Notification Service (direct): http://localhost:4042"
echo "  Payment Service (direct): http://localhost:8090"
echo ""
echo "🗄️  Database Access:"
echo "  Booking DB: localhost:5436"
echo "  Ticket DB: localhost:5437"
echo "  Auth DB: localhost:5438"
echo "  Events DB: localhost:5439"
echo "  Notification DB: localhost:5440"
echo ""
echo "🐰 Infrastructure:"
echo "  RabbitMQ Management: http://localhost:15672"
echo "  Redis: localhost:6379"
echo ""
echo "📊 Monitoring:"
echo "  Jaeger UI: http://localhost:30686"
echo ""
echo "💡 Use 'tilt up' for development environment with auto-reload"
echo "📖 Check k8s/README.md for detailed documentation"
