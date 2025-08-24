#!/bin/bash

# ====== LABASSISTANT OPP PRODUCTION DEPLOYMENT SCRIPT ======
# This script deploys the LabAssistant OPP system with RAG services to production

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROJECT_NAME="labopp"
COMPOSE_FILE="docker-compose.yml"
ENV_FILE=".env.production"

# Logging function
log() {
    echo -e "${GREEN}[$(date +'%Y-%m-%d %H:%M:%S')] $1${NC}"
}

warn() {
    echo -e "${YELLOW}[$(date +'%Y-%m-%d %H:%M:%S')] WARNING: $1${NC}"
}

error() {
    echo -e "${RED}[$(date +'%Y-%m-%d %H:%M:%S')] ERROR: $1${NC}"
    exit 1
}

# Check prerequisites
check_prerequisites() {
    log "Checking prerequisites..."
    
    # Check if Docker is installed
    if ! command -v docker &> /dev/null; then
        error "Docker is not installed. Please install Docker first."
    fi
    
    # Check if Docker Compose is installed
    if ! command -v docker-compose &> /dev/null; then
        error "Docker Compose is not installed. Please install Docker Compose first."
    fi
    
    # Check if environment file exists
    if [ ! -f "$ENV_FILE" ]; then
        warn "Environment file $ENV_FILE not found. Creating from template..."
        if [ -f "env.production.template" ]; then
            cp env.production.template "$ENV_FILE"
            error "Please edit $ENV_FILE with your production values before continuing."
        else
            error "No environment template found. Please create $ENV_FILE manually."
        fi
    fi
    
    log "Prerequisites check completed."
}

# Backup existing data
backup_data() {
    log "Creating backup of existing data..."
    
    BACKUP_DIR="backup_$(date +%Y%m%d_%H%M%S)"
    mkdir -p "$BACKUP_DIR"
    
    # Backup volumes if they exist
    if docker volume ls | grep -q "${PROJECT_NAME}_qdrant_data"; then
        log "Backing up Qdrant data..."
        docker run --rm -v "${PROJECT_NAME}_qdrant_data:/data" -v "$(pwd)/$BACKUP_DIR:/backup" alpine tar czf /backup/qdrant_data.tar.gz -C /data .
    fi
    
    if docker volume ls | grep -q "${PROJECT_NAME}_ollama_data"; then
        log "Backing up Ollama data..."
        docker run --rm -v "${PROJECT_NAME}_ollama_data:/data" -v "$(pwd)/$BACKUP_DIR:/backup" alpine tar czf /backup/ollama_data.tar.gz -C /data .
    fi
    
    log "Backup completed: $BACKUP_DIR"
}

# Stop existing services
stop_services() {
    log "Stopping existing services..."
    docker-compose -f "$COMPOSE_FILE" down --remove-orphans
    log "Services stopped."
}

# Pull latest images
pull_images() {
    log "Pulling latest images..."
    docker-compose -f "$COMPOSE_FILE" pull
    log "Images pulled successfully."
}

# Build services
build_services() {
    log "Building services..."
    docker-compose -f "$COMPOSE_FILE" build --no-cache
    log "Services built successfully."
}

# Start services
start_services() {
    log "Starting services..."
    docker-compose -f "$COMPOSE_FILE" up -d
    
    # Wait for services to be healthy
    log "Waiting for services to be healthy..."
    sleep 30
    
    # Check service health
    check_service_health
}

# Check service health
check_service_health() {
    log "Checking service health..."
    
    # Check Qdrant
    if curl -f http://localhost:6333/collections > /dev/null 2>&1; then
        log "✓ Qdrant is healthy"
    else
        warn "⚠ Qdrant health check failed"
    fi
    
    # Check Ollama
    if curl -f http://localhost:11434/api/tags > /dev/null 2>&1; then
        log "✓ Ollama is healthy"
    else
        warn "⚠ Ollama health check failed"
    fi
    
    # Check RAG Service
    if curl -f http://localhost:3001/health > /dev/null 2>&1; then
        log "✓ RAG Service is healthy"
    else
        warn "⚠ RAG Service health check failed"
    fi
    
    # Check Web API
    if curl -f http://localhost:5000/health > /dev/null 2>&1; then
        log "✓ Web API is healthy"
    else
        warn "⚠ Web API health check failed"
    fi
}

# Initialize RAG models
initialize_rag_models() {
    log "Initializing RAG models..."
    
    # Wait for Ollama to be ready
    log "Waiting for Ollama to be ready..."
    for i in {1..30}; do
        if curl -f http://localhost:11434/api/tags > /dev/null 2>&1; then
            break
        fi
        sleep 2
    done
    
    # Pull required models
    log "Pulling required Ollama models..."
    curl -X POST http://localhost:11434/api/pull -d '{"name": "nomic-embed-text"}'
    curl -X POST http://localhost:11434/api/pull -d '{"name": "llama3.2:3b"}'
    
    log "RAG models initialized."
}

# Show deployment status
show_status() {
    log "Deployment completed successfully!"
    echo
    echo -e "${BLUE}=== SERVICE STATUS ===${NC}"
    docker-compose -f "$COMPOSE_FILE" ps
    echo
    echo -e "${BLUE}=== SERVICE URLS ===${NC}"
    echo "Web API: http://localhost:5000"
    echo "RAG Service: http://localhost:3001"
    echo "Qdrant: http://localhost:6333"
    echo "Ollama: http://localhost:11434"
    echo
    echo -e "${BLUE}=== HEALTH CHECKS ===${NC}"
    echo "Web API Health: http://localhost:5000/health"
    echo "RAG Service Health: http://localhost:3001/health"
    echo "Qdrant Collections: http://localhost:6333/collections"
    echo "Ollama Models: http://localhost:11434/api/tags"
    echo
    echo -e "${YELLOW}Remember to:${NC}"
    echo "1. Configure your firewall rules"
    echo "2. Set up SSL/TLS certificates"
    echo "3. Configure monitoring and alerting"
    echo "4. Set up regular backups"
    echo "5. Monitor resource usage"
}

# Main deployment function
main() {
    log "Starting LabAssistant OPP Production Deployment..."
    
    check_prerequisites
    backup_data
    stop_services
    pull_images
    build_services
    start_services
    initialize_rag_models
    show_status
    
    log "Deployment completed!"
}

# Handle script arguments
case "${1:-deploy}" in
    "deploy")
        main
        ;;
    "stop")
        stop_services
        ;;
    "start")
        start_services
        ;;
    "restart")
        stop_services
        start_services
        ;;
    "status")
        docker-compose -f "$COMPOSE_FILE" ps
        ;;
    "logs")
        docker-compose -f "$COMPOSE_FILE" logs -f
        ;;
    "health")
        check_service_health
        ;;
    "backup")
        backup_data
        ;;
    *)
        echo "Usage: $0 {deploy|stop|start|restart|status|logs|health|backup}"
        echo "  deploy  - Full deployment (default)"
        echo "  stop    - Stop all services"
        echo "  start   - Start all services"
        echo "  restart - Restart all services"
        echo "  status  - Show service status"
        echo "  logs    - Show service logs"
        echo "  health  - Check service health"
        echo "  backup  - Create backup"
        exit 1
        ;;
esac
