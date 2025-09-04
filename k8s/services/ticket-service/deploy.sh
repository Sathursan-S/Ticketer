#!/bin/bash

# Ticket Service Kubernetes Deployment Script
# This script deploys all Ticket Service components to Kubernetes

echo "🚀 Deploying Ticket Service to Kubernetes..."

# Create namespace if it doesn't exist
kubectl create namespace ticketer --dry-run=client -o yaml | kubectl apply -f -

# Apply ConfigMaps and Secrets first
echo "📋 Applying ConfigMaps..."
kubectl apply -f k8s/services/ticket-service/ticket-service-configmap.yaml

# Apply Persistent Volume Claims
echo "💾 Applying Persistent Volume Claims..."
kubectl apply -f k8s/services/ticket-service/ticket-db-pvc.yaml
kubectl apply -f k8s/services/ticket-service/redis-pvc.yaml

# Apply Database and Redis deployments
echo "🗄️  Applying Database and Redis deployments..."
kubectl apply -f k8s/services/ticket-service/ticket-db-deployment.yaml
kubectl apply -f k8s/services/ticket-service/ticket-db-service.yaml
kubectl apply -f k8s/services/ticket-service/redis-deployment.yaml
kubectl apply -f k8s/services/ticket-service/redis-service.yaml

# Wait for database and Redis to be ready
echo "⏳ Waiting for database and Redis to be ready..."
kubectl wait --for=condition=available --timeout=300s deployment/ticket-db
kubectl wait --for=condition=available --timeout=300s deployment/redis

# Apply Ticket Service deployment and service
echo "🎫 Applying Ticket Service..."
kubectl apply -f k8s/services/ticket-service/deployment.yaml
kubectl apply -f k8s/services/ticket-service/service.yaml

# Wait for Ticket Service to be ready
echo "⏳ Waiting for Ticket Service to be ready..."
kubectl wait --for=condition=available --timeout=300s deployment/ticketservice

echo "✅ Ticket Service deployment completed!"
echo ""
echo "📊 Service Status:"
kubectl get pods -l app=ticketservice
kubectl get svc ticketservice
echo ""
echo "🔗 Access URLs:"
echo "  Ticket Service: http://localhost:8080"
echo "  Swagger UI: http://localhost:8080"
echo "  Health Check: http://localhost:8080/health/live"
