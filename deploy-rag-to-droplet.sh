#!/bin/bash

# ====== DEPLOY RAG SERVICE TO EXISTING DROPLET ======
# This script adds RAG services to an existing LabAssistant OPP deployment

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
COMPOSE_FILE="docker-compose.yml"
BACKUP_COMPOSE="docker-compose.backup.yml"

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

success() {
    echo -e "${GREEN}✓ $1${NC}"
}

# Check current deployment
check_current_deployment() {
    log "Checking current deployment status..."
    
    if [ ! -f "$COMPOSE_FILE" ]; then
        error "Docker Compose file not found. Please run this script from the project directory."
    fi
    
    # Check if services are running
    if docker-compose ps | grep -q "Up"; then
        success "Current services are running"
        docker-compose ps
    else
        warn "No services are currently running"
    fi
    
    # Check available ports
    log "Checking port availability..."
    local ports=(3001 6333 6334 11434)
    for port in "${ports[@]}"; do
        if netstat -tuln | grep -q ":$port "; then
            warn "Port $port is already in use"
        else
            success "Port $port is available"
        fi
    done
}

# Backup current configuration
backup_current_config() {
    log "Creating backup of current configuration..."
    
    if [ -f "$COMPOSE_FILE" ]; then
        cp "$COMPOSE_FILE" "$BACKUP_COMPOSE"
        success "Backup created: $BACKUP_COMPOSE"
    fi
    
    # Backup current environment
    if [ -f ".env" ]; then
        cp ".env" ".env.backup.$(date +%Y%m%d_%H%M%S)"
        success "Environment backup created"
    fi
}

# Update docker-compose.yml for RAG services
update_compose_file() {
    log "Updating Docker Compose configuration for RAG services..."
    
    # Check if RAG services already exist
    if grep -q "rag-service:" "$COMPOSE_FILE"; then
        warn "RAG services already exist in docker-compose.yml"
        return 0
    fi
    
    # Create a minimal RAG configuration
    cat >> "$COMPOSE_FILE" << 'EOF'

  # ====== RAG INFRASTRUCTURE ======
  qdrant:
    image: qdrant/qdrant:latest
    container_name: labopp_qdrant
    restart: always
    ports:
      - "6333:6333"
      - "6334:6334"
    volumes:
      - qdrant_data:/qdrant/storage
    environment:
      - QDRANT__SERVICE__HTTP_PORT=6333
      - QDRANT__SERVICE__GRPC_PORT=6334
      - QDRANT__SERVICE__ENABLE_TLS=false
      - QDRANT__SERVICE__CORS_ALLOW_ORIGINS=["*"]
    healthcheck:
      test: ["CMD", "wget", "-qO-", "http://localhost:6333/collections"]
      interval: 30s
      timeout: 10s
      retries: 5
      start_period: 40s
    networks:
      - labopp_network

  ollama:
    image: ollama/ollama:latest
    container_name: labopp_ollama
    restart: always
    ports:
      - "11434:11434"
    environment:
      - OLLAMA_HOST=0.0.0.0
      - OLLAMA_ORIGINS=*
    volumes:
      - ollama_data:/root/.ollama
    healthcheck:
      test: ["CMD", "wget", "-qO-", "http://localhost:11434/api/tags"]
      interval: 30s
      timeout: 10s
      retries: 5
      start_period: 60s
    networks:
      - labopp_network

  rag-service:
    build:
      context: ./rag-cli-nodejs
      dockerfile: Dockerfile
    container_name: labopp_rag_service
    restart: always
    ports:
      - "3001:3001"
    environment:
      - NODE_ENV=production
      - NODE_OPTIONS=--max-old-space-size=2048
      - QDRANT_URL=http://qdrant:6333
      - OLLAMA_URL=http://ollama:11434
      - GOOGLE_API_KEY=${GOOGLE_API_KEY:-AIzaSyDxFNK8N6Y9bkLkNwhoENVhq-gNHH3UrnY}
      - GENAI_MODEL=${GENAI_MODEL:-gemini-1.5-flash-latest}
      - GENAI_BASE=https://generativelanguage.googleapis.com
      - PORT=3001
      - HOST=0.0.0.0
      - CORS_ORIGIN=*
      - LOG_LEVEL=info
      - REQUEST_TIMEOUT=120000
      - EMBEDDING_TIMEOUT=30000
    volumes:
      - rag_uploads:/app/uploads
      - rag_logs:/app/logs
    healthcheck:
      test: ["CMD", "wget", "-qO-", "http://localhost:3001/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 30s
    depends_on:
      qdrant:
        condition: service_healthy
      ollama:
        condition: service_healthy
    networks:
      - labopp_network
    deploy:
      resources:
        limits:
          memory: 2G
          cpus: '1.0'
        reservations:
          memory: 512M
          cpus: '0.5'

volumes:
  qdrant_data:
    driver: local
  ollama_data:
    driver: local
  rag_uploads:
    driver: local
  rag_logs:
    driver: local

networks:
  labopp_network:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16
EOF

    success "Docker Compose configuration updated"
}

# Update Web API environment for RAG integration
update_webapi_config() {
    log "Updating Web API configuration for RAG integration..."
    
    # Check if RAG environment variables already exist
    if grep -q "AIServices__RAGServiceUrl" "$COMPOSE_FILE"; then
        success "RAG environment variables already configured"
        return 0
    fi
    
    # Add RAG environment variables to webapi service
    sed -i '/# AI Services/a\
      # AI Services\
      - AIServices__RAGServiceUrl=http://rag-service:3001\
      - AIServices__AIServiceUrl=http://localhost:3000\
      - AIServices__GeminiApiKey=${GOOGLE_API_KEY:-AIzaSyDxFNK8N6Y9bkLkNwhoENVhq-gNHH3UrnY}' "$COMPOSE_FILE"
    
    # Add rag-service to webapi depends_on
    sed -i '/depends_on:/a\
      - rag-service' "$COMPOSE_FILE"
    
    success "Web API configuration updated"
}

# Deploy RAG services
deploy_rag_services() {
    log "Deploying RAG services..."
    
    # Pull images
    log "Pulling RAG service images..."
    docker-compose pull qdrant ollama
    
    # Build RAG service
    log "Building RAG service..."
    docker-compose build rag-service
    
    # Start RAG services
    log "Starting RAG services..."
    docker-compose up -d qdrant ollama rag-service
    
    # Wait for services to be healthy
    log "Waiting for RAG services to be healthy..."
    sleep 60
    
    # Check service health
    check_rag_health
}

# Check RAG service health
check_rag_health() {
    log "Checking RAG service health..."
    
    # Check Qdrant
    if curl -f http://localhost:6333/collections > /dev/null 2>&1; then
        success "Qdrant is healthy"
    else
        warn "Qdrant health check failed"
    fi
    
    # Check Ollama
    if curl -f http://localhost:11434/api/tags > /dev/null 2>&1; then
        success "Ollama is healthy"
    else
        warn "Ollama health check failed"
    fi
    
    # Check RAG Service
    if curl -f http://localhost:3001/health > /dev/null 2>&1; then
        success "RAG Service is healthy"
    else
        warn "RAG Service health check failed"
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

# Test RAG integration
test_rag_integration() {
    log "Testing RAG integration..."
    
    # Test RAG service endpoints
    local endpoints=("/health" "/ingest" "/review" "/suggest-testcases")
    
    for endpoint in "${endpoints[@]}"; do
        if curl -f http://localhost:3001$endpoint > /dev/null 2>&1; then
            success "RAG Service $endpoint is accessible"
        else
            warn "RAG Service $endpoint is not accessible"
        fi
    done
    
    # Test Web API AI endpoints
    if curl -f http://localhost:5000/api/ai/suggest-testcases > /dev/null 2>&1; then
        success "Web API AI endpoint is accessible"
    else
        warn "Web API AI endpoint is not accessible"
    fi
}

# Show deployment summary
show_summary() {
    log "RAG Service deployment completed!"
    echo
    echo -e "${BLUE}=== NEW SERVICE URLS ===${NC}"
    echo "RAG Service: http://localhost:3001"
    echo "Qdrant: http://localhost:6333"
    echo "Ollama: http://localhost:11434"
    echo
    echo -e "${BLUE}=== HEALTH CHECKS ===${NC}"
    echo "RAG Service Health: http://localhost:3001/health"
    echo "Qdrant Collections: http://localhost:6333/collections"
    echo "Ollama Models: http://localhost:11434/api/tags"
    echo
    echo -e "${BLUE}=== API ENDPOINTS ===${NC}"
    echo "PDF Ingest: POST http://localhost:3001/ingest"
    echo "Code Review: POST http://localhost:3001/review"
    echo "Test Cases: POST http://localhost:3001/suggest-testcases"
    echo
    echo -e "${YELLOW}Next steps:${NC}"
    echo "1. Test the AI features in your application"
    echo "2. Monitor resource usage: docker stats"
    echo "3. Check logs: docker-compose logs rag-service"
    echo "4. Configure firewall rules for new ports"
    echo "5. Set up monitoring for new services"
}

# Rollback function
rollback() {
    log "Rolling back changes..."
    
    # Stop RAG services
    docker-compose stop rag-service qdrant ollama 2>/dev/null || true
    
    # Restore backup
    if [ -f "$BACKUP_COMPOSE" ]; then
        mv "$BACKUP_COMPOSE" "$COMPOSE_FILE"
        success "Configuration restored from backup"
    fi
    
    # Restart original services
    docker-compose up -d
    
    log "Rollback completed"
}

# Main deployment function
main() {
    echo -e "${BLUE}================================${NC}"
    echo -e "${BLUE}  RAG SERVICE DEPLOYMENT${NC}"
    echo -e "${BLUE}================================${NC}"
    echo
    
    check_current_deployment
    backup_current_config
    update_compose_file
    update_webapi_config
    deploy_rag_services
    initialize_rag_models
    test_rag_integration
    show_summary
    
    log "RAG Service deployment completed successfully!"
}

# Handle script arguments
case "${1:-deploy}" in
    "deploy")
        main
        ;;
    "rollback")
        rollback
        ;;
    "check")
        check_current_deployment
        check_rag_health
        ;;
    "test")
        test_rag_integration
        ;;
    "init-models")
        initialize_rag_models
        ;;
    *)
        echo "Usage: $0 {deploy|rollback|check|test|init-models}"
        echo "  deploy      - Deploy RAG services (default)"
        echo "  rollback    - Rollback to previous configuration"
        echo "  check       - Check current deployment and RAG health"
        echo "  test        - Test RAG integration"
        echo "  init-models - Initialize Ollama models"
        exit 1
        ;;
esac
