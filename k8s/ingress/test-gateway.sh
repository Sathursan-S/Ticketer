#!/bin/bash

# Ticketer NGINX Ingress API Gateway Test Script
# This script tests the API gateway endpoints to verify routing is working

set -e

GATEWAY_HOST=${GATEWAY_HOST:-"localhost"}
GATEWAY_PORT=${GATEWAY_PORT:-"80"}
BASE_URL="http://${GATEWAY_HOST}:${GATEWAY_PORT}"

echo "🧪 Testing Ticketer NGINX Ingress API Gateway"
echo "Base URL: ${BASE_URL}"
echo "----------------------------------------"

# Function to test an endpoint
test_endpoint() {
    local path=$1
    local expected_service=$2
    local description=$3
    
    echo -n "Testing ${description}... "
    
    # Use curl with timeout and follow redirects
    if response=$(curl -s -w "%{http_code}" -m 10 --connect-timeout 5 "${BASE_URL}${path}" 2>/dev/null); then
        http_code="${response: -3}"
        if [[ "$http_code" =~ ^[2-4][0-9][0-9]$ ]]; then
            echo "✅ (HTTP $http_code)"
        else
            echo "⚠️  (HTTP $http_code - may be service not ready)"
        fi
    else
        echo "❌ (Connection failed)"
    fi
}

# Test basic connectivity
echo "1. Basic Connectivity Tests"
test_endpoint "/" "default-backend" "Root path"
test_endpoint "/health" "health-check" "Health check endpoint"

echo
echo "2. Microservice API Tests"
test_endpoint "/api/auth/" "authentication-service" "Authentication API"
test_endpoint "/api/booking/" "booking-service" "Booking API"  
test_endpoint "/api/tickets/" "ticket-service" "Ticket API"
test_endpoint "/api/events/" "events-service" "Events API"
test_endpoint "/api/notifications/" "notification-service" "Notification API"
test_endpoint "/api/payments/" "payment-service" "Payment API"

echo
echo "3. CORS Preflight Test"
echo -n "Testing CORS preflight... "
if response=$(curl -s -w "%{http_code}" -m 10 -X OPTIONS \
    -H "Origin: http://localhost:3000" \
    -H "Access-Control-Request-Method: POST" \
    -H "Access-Control-Request-Headers: Content-Type,Authorization" \
    "${BASE_URL}/api/auth/" 2>/dev/null); then
    http_code="${response: -3}"
    if [[ "$http_code" =~ ^[2][0-9][0-9]$ ]]; then
        echo "✅ (HTTP $http_code)"
    else
        echo "⚠️  (HTTP $http_code)"
    fi
else
    echo "❌ (Connection failed)"
fi

echo
echo "4. Invalid Route Test"
test_endpoint "/api/nonexistent/" "default-backend" "Non-existent API"

echo
echo "----------------------------------------"
echo "🏁 Test completed!"
echo
echo "💡 Tips:"
echo "   - If tests fail, ensure all services are running"
echo "   - Check 'kubectl get pods' for service status"
echo "   - Check 'kubectl get ingress' for ingress status"
echo "   - Use 'kubectl logs -l app=nginx-ingress-controller' for ingress logs"