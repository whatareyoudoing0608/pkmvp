#!/bin/bash

################################################################################
# Deploy All Script
# Automatically builds and deploys both Frontend and Backend
################################################################################

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
FE_IMAGE_NAME="pkmvp-frontend"
BE_IMAGE_NAME="pkmvp-backend"
IMAGE_TAG="latest"
FE_CONTAINER_NAME="pkmvp-frontend"
BE_CONTAINER_NAME="pkmvp-backend"
FE_PORT="8085"
BE_API_PORT="8084"
BE_HTTPS_PORT="5001"

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

log_step() {
    echo -e "${BLUE}[STEP]${NC} $1"
}

print_header() {
    echo ""
    echo "╔════════════════════════════════════════════════════╗"
    echo "║  $1"
    echo "╚════════════════════════════════════════════════════╝"
    echo ""
}

print_section() {
    echo ""
    echo "────────────────────────────────────────────────────"
    echo "  $1"
    echo "────────────────────────────────────────────────────"
    echo ""
}

update_source() {
    print_section "Updating Source Code"

    if ! command -v git &> /dev/null; then
        log_error "Git is not installed!"
        return 1
    fi

    if [ ! -d ".git" ]; then
        log_error "Current directory is not a git repository!"
        return 1
    fi

    log_step "Pulling latest code from remote"
    git fetch --all --prune
    git pull --ff-only
    log_info "✓ Source code updated"
}

cleanup_containers() {
    print_section "Cleaning up existing containers"

    # Stop and remove frontend
    if docker ps -a --format '{{.Names}}' | grep -q "^${FE_CONTAINER_NAME}$"; then
        log_warn "Found existing frontend container: ${FE_CONTAINER_NAME}"
        log_info "Stopping..."
        docker stop ${FE_CONTAINER_NAME} || true
        
        log_info "Removing..."
        docker rm ${FE_CONTAINER_NAME} || true
    fi

    # Stop and remove backend
    if docker ps -a --format '{{.Names}}' | grep -q "^${BE_CONTAINER_NAME}$"; then
        log_warn "Found existing backend container: ${BE_CONTAINER_NAME}"
        log_info "Stopping..."
        docker stop ${BE_CONTAINER_NAME} || true
        
        log_info "Removing..."
        docker rm ${BE_CONTAINER_NAME} || true
    fi
}

create_network() {
    print_section "Creating Docker network"
    
    if docker network ls --format '{{.Name}}' | grep -q "^pkmvp-network$"; then
        log_info "Network 'pkmvp-network' already exists"
    else
        log_step "Creating network: pkmvp-network"
        if docker network create pkmvp-network; then
            log_info "✓ Network created successfully"
        else
            log_error "Failed to create network"
            return 1
        fi
    fi
}

build_frontend() {
    print_section "Building Frontend Image"
    
    log_step "Verifying Dockerfile.fe exists..."
    if [ ! -f "Dockerfile.fe" ]; then
        log_error "Dockerfile.fe not found!"
        return 1
    fi

    log_step "Building: ${FE_IMAGE_NAME}:${IMAGE_TAG}"
    log_info "This may take a few minutes..."
    
    if docker build -f Dockerfile.fe -t ${FE_IMAGE_NAME}:${IMAGE_TAG} --no-cache .; then
        log_info "✓ Frontend image built successfully"
        return 0
    else
        log_error "Failed to build frontend image"
        return 1
    fi
}

build_backend() {
    print_section "Building Backend Image"
    
    log_step "Verifying Dockerfile.be exists..."
    if [ ! -f "Dockerfile.be" ]; then
        log_error "Dockerfile.be not found!"
        return 1
    fi

    log_step "Building: ${BE_IMAGE_NAME}:${IMAGE_TAG}"
    log_info "This may take several minutes (compiling .NET)..."
    
    if docker build -f Dockerfile.be -t ${BE_IMAGE_NAME}:${IMAGE_TAG} --no-cache .; then
        log_info "✓ Backend image built successfully"
        return 0
    else
        log_error "Failed to build backend image"
        return 1
    fi
}

start_backend() {
    print_section "Starting Backend Container"
    
    log_step "Starting: ${BE_CONTAINER_NAME}"
    docker run -d \
        --name ${BE_CONTAINER_NAME} \
        -p ${BE_API_PORT}:80 \
        -p ${BE_HTTPS_PORT}:443 \
        -e ASPNETCORE_ENVIRONMENT=Production \
        -e ASPNETCORE_URLS=http://+:80 \
        --restart unless-stopped \
        --network pkmvp-network \
        ${BE_IMAGE_NAME}:${IMAGE_TAG}

    if [ $? -eq 0 ]; then
        log_info "✓ Backend container started"
    else
        log_error "Failed to start backend container"
        return 1
    fi

    # Wait for backend to be ready
    log_info "Waiting for backend to start..."
    sleep 5

    if ! docker ps --format '{{.Names}}' | grep -q "^${BE_CONTAINER_NAME}$"; then
        log_error "Backend container is not running!"
        docker logs ${BE_CONTAINER_NAME}
        return 1
    fi

    log_info "✓ Backend is running on port ${BE_API_PORT}"
}

start_frontend() {
    print_section "Starting Frontend Container"
    
    log_step "Starting: ${FE_CONTAINER_NAME}"
    docker run -d \
        --name ${FE_CONTAINER_NAME} \
        -p ${FE_PORT}:80 \
        --restart unless-stopped \
        --network pkmvp-network \
        ${FE_IMAGE_NAME}:${IMAGE_TAG}

    if [ $? -eq 0 ]; then
        log_info "✓ Frontend container started"
    else
        log_error "Failed to start frontend container"
        return 1
    fi

    # Wait for frontend to be ready
    log_info "Waiting for frontend to start..."
    sleep 3

    if ! docker ps --format '{{.Names}}' | grep -q "^${FE_CONTAINER_NAME}$"; then
        log_error "Frontend container is not running!"
        docker logs ${FE_CONTAINER_NAME}
        return 1
    fi

    log_info "✓ Frontend is running on port ${FE_PORT}"
}

verify_health() {
    print_section "Verifying Services"

    local fe_running=false
    local be_running=false

    # Check frontend
    if docker ps --format '{{.Names}}' | grep -q "^${FE_CONTAINER_NAME}$"; then
        log_info "✓ Frontend container is running"
        fe_running=true
    else
        log_error "✗ Frontend container is not running"
    fi

    # Check backend
    if docker ps --format '{{.Names}}' | grep -q "^${BE_CONTAINER_NAME}$"; then
        log_info "✓ Backend container is running"
        be_running=true
    else
        log_error "✗ Backend container is not running"
    fi

    if [ "$fe_running" = false ] || [ "$be_running" = false ]; then
        return 1
    fi

    return 0
}

show_summary() {
    print_header "✓ Deployment Complete!"
    
    local fe_url="http://localhost:${FE_PORT}"
    local be_url="http://localhost:${BE_API_PORT}"
    local swagger_url="http://localhost:${BE_API_PORT}/swagger"

    echo -e "${GREEN}Services Information:${NC}"
    echo ""
    echo "  Frontend:"
    echo "    Container: ${FE_CONTAINER_NAME}"
    echo "    Image: ${FE_IMAGE_NAME}:${IMAGE_TAG}"
    echo "    Port: ${FE_PORT}"
    echo "    URL: ${fe_url}"
    echo ""
    echo "  Backend:"
    echo "    Container: ${BE_CONTAINER_NAME}"
    echo "    Image: ${BE_IMAGE_NAME}:${IMAGE_TAG}"
    echo "    HTTP Port: ${BE_API_PORT}"
    echo "    HTTPS Port: ${BE_HTTPS_PORT}"
    echo "    API URL: ${be_url}"
    echo "    Swagger: ${swagger_url}"
    echo ""
    
    echo -e "${GREEN}Useful Commands:${NC}"
    echo "  View frontend logs:   docker logs -f ${FE_CONTAINER_NAME}"
    echo "  View backend logs:    docker logs -f ${BE_CONTAINER_NAME}"
    echo "  Stop frontend:        docker stop ${FE_CONTAINER_NAME}"
    echo "  Stop backend:         docker stop ${BE_CONTAINER_NAME}"
    echo "  Stop all services:    docker-compose down"
    echo "  View all containers:  docker ps"
    echo ""
    
    echo -e "${YELLOW}Next Steps:${NC}"
    echo "  1. Open ${fe_url} in your browser"
    echo "  2. Access API: ${be_url}/api/..."
    echo "  3. View API docs: ${swagger_url}"
    echo ""
}

# Main function
main() {
    print_header "PKMVP Full Stack Deployment"

    # Verify prerequisites
    log_info "Checking prerequisites..."
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed!"
        exit 1
    fi
    if ! command -v docker-compose &> /dev/null; then
        log_warn "Docker Compose not found (optional)"
    fi

    # Pull latest source code before deployment
    update_source || exit 1

    # Cleanup
    cleanup_containers

    # Create network
    create_network || exit 1

    # Build images
    build_backend || exit 1
    build_frontend || exit 1

    # Start containers (backend first)
    start_backend || exit 1
    start_frontend || exit 1

    # Verify
    verify_health || exit 1

    # Show summary
    show_summary
}

# Run main function
main
