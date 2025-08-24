#!/bin/bash

# ====== APPLY OPTIMIZED RAG CONFIGURATION ======
# This script applies the optimized configuration for RAG services

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
CURRENT_COMPOSE="docker-compose.yml"
OPTIMIZED_COMPOSE="docker-compose-optimized.yml"
BACKUP_COMPOSE="docker-compose.backup.$(date +%Y%m%d_%H%M%S).yml"

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

# Check current configuration
check_current_config() {
    log "Checking current configuration..."
    
    if [ ! -f "$CURRENT_COMPOSE" ]; then
        error "Current docker-compose.yml not found"
    fi
    
    if [ ! -f "$OPTIMIZED_COMPOSE" ]; then
        error "Optimized docker-compose-optimized.yml not found"
    fi
    
    success "Configuration files found"
}

# Create backup
create_backup() {
    log "Creating backup of current configuration..."
    
    cp "$CURRENT_COMPOSE" "$BACKUP_COMPOSE"
    success "Backup created: $BACKUP_COMPOSE"
}

# Show differences
show_differences() {
    log "Showing key differences between configurations..."
    
    echo -e "${BLUE}=== KEY IMPROVEMENTS ===${NC}"
    echo "1. Qdrant version: 1.9.0 (specific version for stability)"
    echo "2. Health check intervals: 10s (faster detection)"
    echo "3. Resource limits: Explicit memory and CPU constraints"
    echo "4. Performance optimizations: Thread limits for Qdrant"
    echo "5. Environment variables: More comprehensive configuration"
    echo "6. Network isolation: Dedicated network with subnet"
    echo "7. Volume management: Proper volume drivers"
    echo "8. Health check improvements: Better error handling"
    
    echo
    echo -e "${YELLOW}Do you want to see the full diff? (y/n)${NC}"
    read -r response
    if [[ "$response" =~ ^[Yy]$ ]]; then
        diff "$CURRENT_COMPOSE" "$OPTIMIZED_COMPOSE" || true
    fi
}

# Apply optimized configuration
apply_optimized_config() {
    log "Applying optimized configuration..."
    
    # Backup current
    create_backup
    
    # Replace with optimized version
    cp "$OPTIMIZED_COMPOSE" "$CURRENT_COMPOSE"
    success "Optimized configuration applied"
    
    # Show what was changed
    echo -e "${BLUE}=== APPLIED OPTIMIZATIONS ===${NC}"
    echo "✅ Qdrant version pinned to 1.9.0"
    echo "✅ Faster health checks (10s intervals)"
    echo "✅ Resource limits configured"
    echo "✅ Performance optimizations enabled"
    echo "✅ Better environment variable management"
    echo "✅ Improved network configuration"
    echo "✅ Enhanced volume management"
}

# Validate configuration
validate_config() {
    log "Validating configuration..."
    
    if docker-compose config > /dev/null 2>&1; then
        success "Configuration is valid"
    else
        error "Configuration validation failed"
    fi
}

# Test deployment
test_deployment() {
    log "Testing deployment..."
    
    # Check if services are running
    if docker-compose ps | grep -q "Up"; then
        warn "Services are currently running. Stopping for update..."
        docker-compose down
    fi
    
    # Start services
    log "Starting services with optimized configuration..."
    docker-compose up -d
    
    # Wait for services to be healthy
    log "Waiting for services to be healthy..."
    sleep 30
    
    # Check health
    check_service_health
}

# Check service health
check_service_health() {
    log "Checking service health..."
    
    # Check Qdrant
    if curl -f http://localhost:6333/collections > /dev/null 2>&1; then
        success "Qdrant is healthy"
    else
        warn "Qdrant health check failed"
    fi
    
    # Check RAG Service
    if curl -f http://localhost:3001/health > /dev/null 2>&1; then
        success "RAG Service is healthy"
    else
        warn "RAG Service health check failed"
    fi
    
    # Check Web API
    if curl -f http://localhost:5000/health > /dev/null 2>&1; then
        success "Web API is healthy"
    else
        warn "Web API health check failed"
    fi
}

# Show performance metrics
show_performance_metrics() {
    log "Showing performance metrics..."
    
    echo -e "${BLUE}=== PERFORMANCE METRICS ===${NC}"
    
    # Memory usage
    echo "Memory Usage:"
    docker stats --no-stream --format "table {{.Container}}\t{{.MemUsage}}\t{{.MemPerc}}" | head -10
    
    echo
    echo "CPU Usage:"
    docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}" | head -10
    
    echo
    echo "Network Usage:"
    docker stats --no-stream --format "table {{.Container}}\t{{.NetIO}}" | head -10
}

# Rollback function
rollback() {
    log "Rolling back to previous configuration..."
    
    if [ -f "$BACKUP_COMPOSE" ]; then
        docker-compose down
        cp "$BACKUP_COMPOSE" "$CURRENT_COMPOSE"
        docker-compose up -d
        success "Rollback completed"
    else
        error "No backup found for rollback"
    fi
}

# Show optimization benefits
show_benefits() {
    echo -e "${BLUE}=== OPTIMIZATION BENEFITS ===${NC}"
    echo "🚀 Performance:"
    echo "   - Faster health checks (10s vs 30s)"
    echo "   - Optimized thread limits for Qdrant"
    echo "   - Better resource management"
    
    echo
    echo "🔒 Stability:"
    echo "   - Pinned Qdrant version (1.9.0)"
    echo "   - Explicit resource limits"
    echo "   - Better error handling"
    
    echo
    echo "📊 Monitoring:"
    echo "   - Enhanced health checks"
    echo "   - Better logging configuration"
    echo "   - Resource usage tracking"
    
    echo
    echo "🛡️ Security:"
    echo "   - Network isolation"
    echo "   - Proper volume management"
    echo "   - Environment variable validation"
}

# Main function
main() {
    echo -e "${BLUE}================================${NC}"
    echo -e "${BLUE}  RAG CONFIGURATION OPTIMIZER${NC}"
    echo -e "${BLUE}================================${NC}"
    echo
    
    check_current_config
    show_differences
    apply_optimized_config
    validate_config
    test_deployment
    show_performance_metrics
    show_benefits
    
    echo
    echo -e "${GREEN}✅ Optimization completed successfully!${NC}"
    echo
    echo -e "${YELLOW}Next steps:${NC}"
    echo "1. Monitor performance: docker stats"
    echo "2. Check logs: docker-compose logs"
    echo "3. Test AI features in your application"
    echo "4. If issues occur, run: $0 rollback"
}

# Handle script arguments
case "${1:-optimize}" in
    "optimize")
        main
        ;;
    "rollback")
        rollback
        ;;
    "check")
        check_current_config
        validate_config
        ;;
    "diff")
        show_differences
        ;;
    "test")
        test_deployment
        ;;
    "metrics")
        show_performance_metrics
        ;;
    *)
        echo "Usage: $0 {optimize|rollback|check|diff|test|metrics}"
        echo "  optimize - Apply optimized configuration (default)"
        echo "  rollback - Rollback to previous configuration"
        echo "  check    - Check and validate configuration"
        echo "  diff     - Show differences between configurations"
        echo "  test     - Test deployment"
        echo "  metrics  - Show performance metrics"
        exit 1
        ;;
esac
