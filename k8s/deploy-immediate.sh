#!/bin/bash

# Immediate Industry Standard Implementations Deployment Script
# This script deploys secrets management, monitoring, and Helm migration

echo "🚀 Deploying Immediate Industry Standard Implementations..."

# Create namespace if it doesn't exist
kubectl create namespace ticketer --dry-run=client -o yaml | kubectl apply -f -

# Deploy secrets first
echo "🔒 Deploying Secrets Management..."
kubectl apply -f k8s/secrets/database-secrets.yaml
kubectl apply -f k8s/secrets/rabbitmq-secrets.yaml

# Deploy monitoring stack
echo "📊 Deploying Monitoring Stack..."
kubectl apply -f k8s/monitoring/prometheus-config.yaml
kubectl apply -f k8s/monitoring/prometheus-deployment.yaml
kubectl apply -f k8s/monitoring/prometheus-service.yaml
kubectl apply -f k8s/monitoring/grafana-deployment.yaml
kubectl apply -f k8s/monitoring/grafana-service.yaml

# Update existing services to use secrets
echo "🔄 Updating Services to use Secrets..."
kubectl apply -f k8s/services/booking-service/

# Wait for monitoring to be ready
echo "⏳ Waiting for monitoring stack to be ready..."
kubectl wait --for=condition=available --timeout=300s deployment/prometheus
kubectl wait --for=condition=available --timeout=300s deployment/grafana

echo "✅ Immediate implementations deployed successfully!"
echo ""
echo "🔗 Access URLs:"
echo "  Prometheus: http://localhost:9090"
echo "  Grafana: http://localhost:3000 (admin/admin)"
echo "  Booking Service: http://localhost:5200"
echo ""
echo "📊 Monitoring Status:"
kubectl get pods -l app=prometheus,app=grafana
echo ""
echo "💡 Use 'tilt up' for development environment with all components"
echo "📖 Check k8s/immediate-implementations.md for detailed documentation"
