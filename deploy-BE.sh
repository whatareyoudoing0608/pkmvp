#!/bin/bash

################################################################################
# Deploy Backend Script
# Automatically builds and deploys .NET Core API
################################################################################

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
IMAGE_NAME="pkmvp-backend"
IMAGE_TAG="latest"
CONTAINER_NAME="pkmvp-backend"
API_PORT="8084"
HTTPS_PORT="5001"
REGISTRY=""  # Set this if using registry (e.g., "myregistry.azurecr.io")

# Functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_header() {
    echo ""
    echo "========================================"
    echo "$1"
    echo "========================================"
    echo ""
}

# Main deployment process
main() {
    print_header "PKMVP Backend Deployment"

    # Step 1: Check if Docker is installed
    log_info "Checking Docker installation..."
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed!"
        exit 1
    fi
    log_info "Docker found: $(docker --version)"

    # Step 2: Verify Dockerfile exists
    log_info "Verifying Dockerfile.be exists..."
    if [ ! -f "Dockerfile.be" ]; then
        log_error "Dockerfile.be not found in current directory!"
        exit 1
    fi

    # Step 3: Stop existing container if running
    log_info "Checking for existing container..."
    if docker ps -a --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
        log_warn "Found existing container: ${CONTAINER_NAME}"
        log_info "Stopping container..."
        docker stop ${CONTAINER_NAME} || true
        
        log_info "Removing container..."
        docker rm ${CONTAINER_NAME} || true
    fi

    # Step 4: Build Docker image
    log_info "Building Docker image: ${IMAGE_NAME}:${IMAGE_TAG}"
    log_info "This may take several minutes (compiling .NET)..."
    
    if docker build -f Dockerfile.be -t ${IMAGE_NAME}:${IMAGE_TAG} .; then
        log_info "✓ Image built successfully"
    else
        log_error "Failed to build image"
        exit 1
    fi

    # Step 5: Tag image for registry (if registry is configured)
    if [ ! -z "$REGISTRY" ]; then
        log_info "Tagging image for registry: ${REGISTRY}/${IMAGE_NAME}:${IMAGE_TAG}"
        docker tag ${IMAGE_NAME}:${IMAGE_TAG} ${REGISTRY}/${IMAGE_NAME}:${IMAGE_TAG}
    fi

    # Step 5b: Create network if not exists
    log_info "Creating Docker network (if needed)..."
    docker network create pkmvp-network 2>/dev/null || true

    # Step 6: Create container
    log_info "Starting container..."
    docker run -d \
        --name ${CONTAINER_NAME} \
        -p ${API_PORT}:80 \
        -p ${HTTPS_PORT}:443 \
        -e ASPNETCORE_ENVIRONMENT=Production \
        -e ASPNETCORE_URLS=http://+:80 \
        --restart unless-stopped \
        --network pkmvp-network \
        ${IMAGE_NAME}:${IMAGE_TAG}

    if [ $? -eq 0 ]; then
        log_info "✓ Container started successfully"
    else
        log_error "Failed to start container"
        exit 1
    fi

    # Step 7: Wait for container to be ready
    log_info "Waiting for API to start..."
    sleep 5

    # Step 8: Verify container is running
    if docker ps --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
        log_info "✓ Container is running"
    else
        log_error "Container is not running!"
        log_info "Container logs:"
        docker logs ${CONTAINER_NAME}
        exit 1
    fi

    # Step 9: Check API health (optional)
    log_info "Testing API health..."
    API_URL="http://localhost:${API_PORT}/health"
    
    if command -v curl &> /dev/null; then
        if curl -s -f ${API_URL} > /dev/null; then
            log_info "✓ API is responding"
        else
            log_warn "API health check inconclusive (container may still be starting)"
        fi
    fi

    # Step 10: Display deployment info
    print_header "Deployment Complete"
    log_info "Backend API is now running!"
    log_info "Container: ${CONTAINER_NAME}"
    log_info "Image: ${IMAGE_NAME}:${IMAGE_TAG}"
    log_info "HTTP Port: ${API_PORT}"
    log_info "HTTPS Port: ${HTTPS_PORT}"
    log_info "API URL: http://localhost:${API_PORT}"
    log_info "Swagger: http://localhost:${API_PORT}/swagger"
    echo ""
    log_info "View logs: docker logs -f ${CONTAINER_NAME}"
    log_info "Stop container: docker stop ${CONTAINER_NAME}"
    echo ""
}

# Run main function
main
