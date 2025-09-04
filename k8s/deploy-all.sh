#!/bin/bash

# Complete Ticketer Microservices Kubernetes Deployment Script
# This script deploys all services and infrastructure to Kubernetes

echo "🚀 Deploying Complete Ticketer Microservices to Kubernetes..."

# Create namespace if it doesn't exist
kubectl create namespace ticketer --dry-run=client -o yaml | kubectl apply -f -

# Deploy infrastructure first
echo "🏗️  Deploying Infrastructure..."
kubectl apply -f k8s/infra/rabbitmq/

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

# Gateway API
echo "🌐 Deploying Gateway API..."
kubectl apply -f k8s/services/gateway-api/

# Wait for all deployments to be ready
echo "⏳ Waiting for all services to be ready..."
kubectl wait --for=condition=available --timeout=600s deployment/bookingservice
kubectl wait --for=condition=available --timeout=600s deployment/ticketservice
kubectl wait --for=condition=available --timeout=600s deployment/authentication-service
kubectl wait --for=condition=available --timeout=600s deployment/events-service
kubectl wait --for=condition=available --timeout=600s deployment/notification-service
kubectl wait --for=condition=available --timeout=600s deployment/payment-service
kubectl wait --for=condition=available --timeout=600s deployment/gateway-api

echo "✅ All services deployed successfully!"
echo ""
echo "📊 Service Status:"
kubectl get pods
echo ""
echo "🔗 Access URLs:"
echo "  Gateway API: http://localhost:5266"
echo "  Booking Service: http://localhost:5200"
echo "  Ticket Service: http://localhost:8080"
echo "  Authentication Service: http://localhost:4040"
echo "  Events Service: http://localhost:4041"
echo "  Notification Service: http://localhost:4042"
echo "  Payment Service: http://localhost:8090"
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
echo "💡 Use 'tilt up' for development environment with auto-reload"
echo "📖 Check k8s/README.md for detailed documentation"
