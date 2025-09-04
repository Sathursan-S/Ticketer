#!/bin/bash

# Test script for Kong API Gateway setup
# This script validates the Kong configuration and tests API endpoints

echo "🧪 Testing Kong API Gateway Configuration..."
echo "=============================================="

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Check prerequisites
echo "📋 Checking Prerequisites..."
if ! command_exists kubectl; then
    echo "❌ kubectl is not installed"
    exit 1
fi

if ! command_exists curl; then
    echo "❌ curl is not installed"
    exit 1
fi

echo "✅ Prerequisites check passed"
echo ""

# Validate Kubernetes manifests
echo "📝 Validating Kubernetes Manifests..."

# Validate Kong manifests
for file in k8s/api-gateway/kong/*.yaml; do
    if [ -f "$file" ]; then
        echo "Validating $file..."
        kubectl apply --dry-run=client --validate=false -f "$file" > /dev/null 2>&1
        if [ $? -eq 0 ]; then
            echo "✅ $file is valid"
        else
            echo "❌ $file has validation errors"
            kubectl apply --dry-run=client --validate=false -f "$file"
        fi
    fi
done

# Validate plugin configurations
echo "Validating k8s/api-gateway/kong-plugins.yaml..."
kubectl apply --dry-run=client --validate=false -f k8s/api-gateway/kong-plugins.yaml > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "✅ kong-plugins.yaml is valid"
else
    echo "❌ kong-plugins.yaml has validation errors"
    kubectl apply --dry-run=client --validate=false -f k8s/api-gateway/kong-plugins.yaml
fi

# Validate Ingress
echo "Validating k8s/api-gateway/ingress.yaml..."
kubectl apply --dry-run=client --validate=false -f k8s/api-gateway/ingress.yaml > /dev/null 2>&1
if [ $? -eq 0 ]; then
    echo "✅ ingress.yaml is valid"
else
    echo "❌ ingress.yaml has validation errors"
    kubectl apply --dry-run=client --validate=false -f k8s/api-gateway/ingress.yaml
fi

echo ""
echo "✅ Configuration validation complete!"
echo ""
echo "📚 Next Steps:"
echo "1. Deploy Kong: ./k8s/deploy-all.sh"
echo "2. Port forward Kong: kubectl port-forward svc/kong-proxy 8000:80 -n kong"
echo "3. Test endpoints:"
echo "   - Health: curl http://localhost:8000/"
echo "   - Auth: curl http://localhost:8000/api/auth/exists?mailId=test@example.com"
echo "   - Kong Admin: curl http://localhost:8001/status"