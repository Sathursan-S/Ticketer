#!/usr/bin/env pwsh
# Script to deploy the Ticketer microservices to Kubernetes

# Check if kubectl is installed
if (-not (Get-Command "kubectl" -ErrorAction SilentlyContinue)) {
    Write-Error "kubectl is not installed. Please install it and try again."
    exit 1
}

# Function to check if a Docker image exists
function Test-DockerImage {
    param (
        [Parameter(Mandatory=$true)]
        [string]$ImageName
    )
    
    $imageExists = docker images -q $ImageName 2>$null
    return $imageExists -ne $null -and $imageExists -ne ""
}

# Build Docker images for all services if they don't exist
$services = @(
    @{Name="booking-service"; Path="./services/BookingService"; Tag="bookingservice:1.0.0"},
    @{Name="ticket-service"; Path="./services/TicketService"; Tag="ticketservice:1.0.0"},
    @{Name="payment-service"; Path="./services/PaymentService"; Tag="paymentservice:1.0.0"},
    @{Name="gateway-api"; Path="./services/Gateway.Api"; Tag="gatewayapi:1.0.0"}
)

Write-Host "Checking and building Docker images if needed..." -ForegroundColor Cyan

foreach ($service in $services) {
    if (-not (Test-DockerImage -ImageName $service.Tag)) {
        Write-Host "Building Docker image for $($service.Name)..." -ForegroundColor Yellow
        docker build -t $service.Tag -f "$($service.Path)/Dockerfile" .
    } else {
        Write-Host "Docker image for $($service.Name) already exists." -ForegroundColor Green
    }
}

# Create the Kubernetes namespace and apply the configuration
Write-Host "Creating Kubernetes resources..." -ForegroundColor Cyan

Write-Host "Applying namespace configuration..." -ForegroundColor Yellow
kubectl apply -f ./k8s/namespace.yaml

Write-Host "Applying configmaps..." -ForegroundColor Yellow
kubectl apply -f ./k8s/configmaps.yaml
kubectl apply -f ./k8s/gateway-config.yaml

Write-Host "Applying storage configuration..." -ForegroundColor Yellow
kubectl apply -f ./k8s/storage.yaml

Write-Host "Applying service configuration..." -ForegroundColor Yellow
kubectl apply -f ./k8s/services.yaml

Write-Host "Applying deployments..." -ForegroundColor Yellow
kubectl apply -f ./k8s/deployments.yaml

Write-Host "Applying ingress configuration..." -ForegroundColor Yellow
kubectl apply -f ./k8s/ingress.yaml

# Wait for all pods to be ready
Write-Host "Waiting for pods to be ready..." -ForegroundColor Yellow
kubectl wait --namespace=ticketer --for=condition=ready pod --all --timeout=300s

# Show the status of all resources in the ticketer namespace
Write-Host "`nDisplaying the status of all resources in the 'ticketer' namespace:" -ForegroundColor Cyan
kubectl get all -n ticketer

# Display ingress information
Write-Host "`nIngress information:" -ForegroundColor Cyan
kubectl get ingress -n ticketer

Write-Host "`nDeployment completed successfully!" -ForegroundColor Green
Write-Host "Access your application at: http://ticketer.local" -ForegroundColor Green
Write-Host "Access RabbitMQ management interface at: http://rabbitmq.ticketer.local" -ForegroundColor Green

Write-Host "`nNote: Make sure to add the following entries to your hosts file:" -ForegroundColor Yellow
Write-Host "127.0.0.1 ticketer.local" -ForegroundColor Yellow
Write-Host "127.0.0.1 rabbitmq.ticketer.local" -ForegroundColor Yellow
