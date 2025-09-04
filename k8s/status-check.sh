#!/bin/bash

# Ticketer Services Status Check Script
# This script checks the status of all deployed services

echo "🔍 Checking Ticketer Microservices Status..."
echo "=============================================="

# Check all pods
echo "📦 Pod Status:"
kubectl get pods --sort-by=.metadata.name
echo ""

# Check all services
echo "🌐 Service Status:"
kubectl get svc --sort-by=.metadata.name
echo ""

# Check deployments
echo "🚀 Deployment Status:"
kubectl get deployments --sort-by=.metadata.name
echo ""

# Check databases
echo "🗄️  Database Pods:"
kubectl get pods -l app=booking-db,ticket-db,auth-db,events-db,notification-db --sort-by=.metadata.name
echo ""

# Check infrastructure
echo "🏗️  Infrastructure:"
kubectl get pods -l app=rabbitmq,redis --sort-by=.metadata.name
echo ""

# Health check summary
echo "❤️  Health Check Endpoints:"
echo "  Gateway API: http://localhost:5266/health"
echo "  Booking Service: http://localhost:5200/health"
echo "  Ticket Service: http://localhost:8080/health/live"
echo "  Payment Service: http://localhost:8090/health"
echo "  Authentication Service: http://localhost:4040/actuator/health"
echo "  Events Service: http://localhost:4041/actuator/health"
echo "  Notification Service: http://localhost:4042/actuator/health"
echo ""

# Port forwarding reminder
echo "🔗 Port Forwarding (if needed):"
echo "  kubectl port-forward svc/gateway-api 5266:80"
echo "  kubectl port-forward svc/bookingservice 5200:80"
echo "  kubectl port-forward svc/ticketservice 8080:8080"
echo "  kubectl port-forward svc/authentication-service 4040:4040"
echo "  kubectl port-forward svc/events-service 4041:4041"
echo "  kubectl port-forward svc/notification-service 4042:4042"
echo "  kubectl port-forward svc/payment-service 8090:8090"
echo ""

# Resource usage
echo "📊 Resource Usage:"
kubectl top pods --sort-by=cpu
echo ""

echo "✅ Status check complete!"
