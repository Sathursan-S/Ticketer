# Troubleshooting Guide - NGINX Ingress API Gateway

This guide helps troubleshoot common issues with the Ticketer NGINX Ingress API Gateway.

## Quick Health Checks

### 1. Check All Pods Status
```bash
kubectl get pods -l app=nginx-ingress-controller
kubectl get pods --show-labels
```

### 2. Check Ingress Status
```bash
kubectl get ingress ticketer-api-gateway
kubectl describe ingress ticketer-api-gateway
```

### 3. Check Services
```bash
kubectl get services
kubectl get service nginx-ingress-controller
```

### 4. Test Gateway Connectivity
```bash
./k8s/ingress/test-gateway.sh
```

## Common Issues and Solutions

### Issue: HTTP 404 - Not Found

**Symptoms:** API requests return 404 errors

**Possible Causes:**
- Ingress rules not properly configured
- Service names don't match
- Path patterns incorrect

**Solutions:**
```bash
# Check ingress configuration
kubectl get ingress -o yaml

# Verify service names match
kubectl get services | grep -E "(authentication|booking|ticket|events|notification|payment)"

# Check ingress controller logs
kubectl logs -l app=nginx-ingress-controller
```

### Issue: HTTP 503 - Service Unavailable

**Symptoms:** Gateway returns 503 errors

**Possible Causes:**
- Backend services not running
- Services not ready
- Database connections failing

**Solutions:**
```bash
# Check all service pods
kubectl get pods --show-labels

# Check specific service logs
kubectl logs -l app=authentication-service
kubectl logs -l app=bookingservice
kubectl logs -l app=ticketservice
kubectl logs -l app=events-service
kubectl logs -l app=notification-service
kubectl logs -l app=payment-service

# Check service health endpoints directly
kubectl port-forward service/authentication-service 4040:4040 &
curl http://localhost:4040/health
```

### Issue: HTTP 502 - Bad Gateway

**Symptoms:** Gateway returns 502 errors

**Possible Causes:**
- Wrong target ports in service definitions
- Services listening on wrong ports
- Network policies blocking traffic

**Solutions:**
```bash
# Check service port configurations
kubectl get services -o wide

# Verify service endpoints
kubectl get endpoints

# Test service connectivity from within cluster
kubectl run test-pod --image=busybox --restart=Never --rm -ti -- sh
# Then inside the pod:
# wget -O- http://authentication-service:4040/health
```

### Issue: CORS Errors

**Symptoms:** Browser console shows CORS errors

**Possible Causes:**
- CORS configuration not applied
- Wrong origin settings

**Solutions:**
```bash
# Check ingress annotations
kubectl get ingress ticketer-api-gateway -o yaml | grep -A 10 annotations

# Test CORS preflight
curl -X OPTIONS \
  -H "Origin: http://localhost:3000" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: Content-Type" \
  http://localhost/api/auth/
```

### Issue: Ingress Controller Not Starting

**Symptoms:** nginx-ingress-controller pod in CrashLoopBackOff

**Possible Causes:**
- RBAC permissions missing
- ConfigMap issues
- Resource conflicts

**Solutions:**
```bash
# Check pod status and events
kubectl describe pod -l app=nginx-ingress-controller

# Check RBAC permissions
kubectl auth can-i '*' '*' --as=system:serviceaccount:default:nginx-ingress-controller

# Check logs
kubectl logs -l app=nginx-ingress-controller --previous
```

## Monitoring Commands

### Check Metrics
```bash
# Access NGINX metrics
kubectl port-forward service/nginx-ingress-controller 10254:10254 &
curl http://localhost:10254/metrics

# Check in Prometheus
curl http://localhost:9090/api/v1/query?query=nginx_ingress_controller_requests_total
```

### Performance Monitoring
```bash
# Watch ingress controller logs
kubectl logs -l app=nginx-ingress-controller -f

# Monitor request patterns
kubectl logs -l app=nginx-ingress-controller | grep -E "(GET|POST|PUT|DELETE)"
```

## Development Tips

### Local Testing Without Cluster
```bash
# Set different host/port for testing
GATEWAY_HOST=your-cluster-ip GATEWAY_PORT=80 ./k8s/ingress/test-gateway.sh
```

### Port Forward for Direct Access
```bash
# Access services directly (bypassing ingress)
kubectl port-forward service/authentication-service 4040:4040
kubectl port-forward service/bookingservice 5200:5200
kubectl port-forward service/ticketservice 8080:8080
```

### Check Configuration Changes
```bash
# Apply configuration changes
kubectl apply -f k8s/ingress/

# Restart ingress controller if needed
kubectl rollout restart deployment/nginx-ingress-controller
```

## Emergency Recovery

If the ingress controller is completely broken:

```bash
# Delete and recreate
kubectl delete -f k8s/ingress/nginx-controller-deployment.yaml
kubectl apply -f k8s/ingress/nginx-controller-deployment.yaml

# Or use Tilt to redeploy everything
tilt down
tilt up
```