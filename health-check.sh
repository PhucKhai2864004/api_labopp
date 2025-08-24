#!/bin/bash

# ====== LABASSISTANT OPP HEALTH CHECK SCRIPT ======
# This script performs comprehensive health checks on the deployed system

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
BASE_URL="http://localhost"
WEBAPI_PORT=5000
RAG_PORT=3001
QDANT_PORT=6333
OLLAMA_PORT=11434

# Logging function
log() {
    echo -e "${GREEN}[$(date +'%Y-%m-%d %H:%M:%S')] $1${NC}"
}

warn() {
    echo -e "${YELLOW}[$(date +'%Y-%m-%d %H:%M:%S')] WARNING: $1${NC}"
}

error() {
    echo -e "${RED}[$(date +'%Y-%m-%d %H:%M:%S')] ERROR: $1${NC}"
}

success() {
    echo -e "${GREEN}✓ $1${NC}"
}

fail() {
    echo -e "${RED}✗ $1${NC}"
}

# Check if service is responding
check_service() {
    local service_name=$1
    local url=$2
    local expected_status=${3:-200}
    
    log "Checking $service_name..."
    
    if curl -f -s "$url" > /dev/null 2>&1; then
        success "$service_name is healthy"
        return 0
    else
        fail "$service_name is not responding"
        return 1
    fi
}

# Check Docker containers
check_containers() {
    log "Checking Docker containers..."
    
    local containers=("laboop_api" "labopp_rag_service" "labopp_qdrant" "labopp_ollama" "java_sandbox")
    local all_healthy=true
    
    for container in "${containers[@]}"; do
        if docker ps --format "table {{.Names}}\t{{.Status}}" | grep -q "$container.*Up"; then
            success "Container $container is running"
        else
            fail "Container $container is not running"
            all_healthy=false
        fi
    done
    
    if [ "$all_healthy" = true ]; then
        success "All containers are running"
    else
        error "Some containers are not running"
        return 1
    fi
}

# Check Web API endpoints
check_webapi() {
    log "Checking Web API endpoints..."
    
    local endpoints=(
        "/health"
        "/api/auth/google"
        "/api/ai/suggest-testcases"
    )
    
    for endpoint in "${endpoints[@]}"; do
        local url="$BASE_URL:$WEBAPI_PORT$endpoint"
        if curl -f -s "$url" > /dev/null 2>&1; then
            success "Web API $endpoint is accessible"
        else
            warn "Web API $endpoint is not accessible"
        fi
    done
}

# Check RAG Service endpoints
check_rag_service() {
    log "Checking RAG Service endpoints..."
    
    local endpoints=(
        "/health"
        "/ingest"
        "/review"
        "/suggest-testcases"
    )
    
    for endpoint in "${endpoints[@]}"; do
        local url="$BASE_URL:$RAG_PORT$endpoint"
        if curl -f -s "$url" > /dev/null 2>&1; then
            success "RAG Service $endpoint is accessible"
        else
            warn "RAG Service $endpoint is not accessible"
        fi
    done
}

# Check Qdrant
check_qdrant() {
    log "Checking Qdrant..."
    
    local url="$BASE_URL:$QDANT_PORT/collections"
    if curl -f -s "$url" > /dev/null 2>&1; then
        success "Qdrant is accessible"
        
        # Check collections
        local collections=$(curl -s "$url" | jq -r '.collections[].name' 2>/dev/null || echo "")
        if [ -n "$collections" ]; then
            success "Qdrant has collections: $collections"
        else
            warn "Qdrant has no collections"
        fi
    else
        fail "Qdrant is not accessible"
    fi
}

# Check Ollama
check_ollama() {
    log "Checking Ollama..."
    
    local url="$BASE_URL:$OLLAMA_PORT/api/tags"
    if curl -f -s "$url" > /dev/null 2>&1; then
        success "Ollama is accessible"
        
        # Check models
        local models=$(curl -s "$url" | jq -r '.models[].name' 2>/dev/null || echo "")
        if [ -n "$models" ]; then
            success "Ollama has models: $models"
        else
            warn "Ollama has no models"
        fi
    else
        fail "Ollama is not accessible"
    fi
}

# Check system resources
check_resources() {
    log "Checking system resources..."
    
    # Check memory usage
    local memory_usage=$(free -m | awk 'NR==2{printf "%.1f%%", $3*100/$2}')
    local memory_total=$(free -m | awk 'NR==2{print $2}')
    local memory_used=$(free -m | awk 'NR==2{print $3}')
    
    echo "Memory Usage: ${memory_used}MB / ${memory_total}MB (${memory_usage})"
    
    if [ "${memory_usage%.*}" -lt 80 ]; then
        success "Memory usage is acceptable"
    else
        warn "Memory usage is high"
    fi
    
    # Check disk usage
    local disk_usage=$(df -h / | awk 'NR==2{print $5}' | sed 's/%//')
    echo "Disk Usage: ${disk_usage}%"
    
    if [ "$disk_usage" -lt 80 ]; then
        success "Disk usage is acceptable"
    else
        warn "Disk usage is high"
    fi
    
    # Check Docker resource usage
    log "Checking Docker resource usage..."
    docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.MemPerc}}"
}

# Check network connectivity
check_network() {
    log "Checking network connectivity..."
    
    # Check internal network
    if docker network ls | grep -q "labopp_network"; then
        success "Docker network exists"
    else
        fail "Docker network not found"
    fi
    
    # Check external connectivity
    if curl -f -s "https://www.google.com" > /dev/null 2>&1; then
        success "External connectivity is working"
    else
        warn "External connectivity issues"
    fi
}

# Check logs for errors
check_logs() {
    log "Checking recent logs for errors..."
    
    local services=("webapi" "rag-service" "qdrant" "ollama")
    local error_found=false
    
    for service in "${services[@]}"; do
        log "Checking $service logs..."
        local errors=$(docker-compose logs --tail=50 "$service" 2>/dev/null | grep -i "error\|exception\|failed" || true)
        
        if [ -n "$errors" ]; then
            warn "Found errors in $service logs:"
            echo "$errors" | head -5
            error_found=true
        else
            success "No recent errors in $service logs"
        fi
    done
    
    if [ "$error_found" = false ]; then
        success "No recent errors found in any service logs"
    fi
}

# Performance test
performance_test() {
    log "Running performance tests..."
    
    # Test Web API response time
    local start_time=$(date +%s%N)
    if curl -f -s "$BASE_URL:$WEBAPI_PORT/health" > /dev/null 2>&1; then
        local end_time=$(date +%s%N)
        local duration=$(( (end_time - start_time) / 1000000 ))
        echo "Web API response time: ${duration}ms"
        
        if [ "$duration" -lt 1000 ]; then
            success "Web API response time is good"
        else
            warn "Web API response time is slow"
        fi
    fi
    
    # Test RAG Service response time
    local start_time=$(date +%s%N)
    if curl -f -s "$BASE_URL:$RAG_PORT/health" > /dev/null 2>&1; then
        local end_time=$(date +%s%N)
        local duration=$(( (end_time - start_time) / 1000000 ))
        echo "RAG Service response time: ${duration}ms"
        
        if [ "$duration" -lt 1000 ]; then
            success "RAG Service response time is good"
        else
            warn "RAG Service response time is slow"
        fi
    fi
}

# Main health check function
main() {
    echo -e "${BLUE}================================${NC}"
    echo -e "${BLUE}  LABASSISTANT OPP HEALTH CHECK${NC}"
    echo -e "${BLUE}================================${NC}"
    echo
    
    local overall_status=0
    
    # Run all checks
    check_containers || overall_status=1
    echo
    
    check_network || overall_status=1
    echo
    
    check_service "Web API" "$BASE_URL:$WEBAPI_PORT/health" || overall_status=1
    check_webapi
    echo
    
    check_service "RAG Service" "$BASE_URL:$RAG_PORT/health" || overall_status=1
    check_rag_service
    echo
    
    check_qdrant || overall_status=1
    echo
    
    check_ollama || overall_status=1
    echo
    
    check_resources
    echo
    
    performance_test
    echo
    
    check_logs
    echo
    
    # Summary
    echo -e "${BLUE}================================${NC}"
    echo -e "${BLUE}  HEALTH CHECK SUMMARY${NC}"
    echo -e "${BLUE}================================${NC}"
    
    if [ "$overall_status" -eq 0 ]; then
        echo -e "${GREEN}✓ All systems are healthy!${NC}"
        echo
        echo -e "${BLUE}Service URLs:${NC}"
        echo "Web API: $BASE_URL:$WEBAPI_PORT"
        echo "RAG Service: $BASE_URL:$RAG_PORT"
        echo "Qdrant: $BASE_URL:$QDANT_PORT"
        echo "Ollama: $BASE_URL:$OLLAMA_PORT"
        echo
        echo -e "${GREEN}System is ready for production use!${NC}"
    else
        echo -e "${RED}✗ Some issues were found. Please review the warnings above.${NC}"
        echo
        echo -e "${YELLOW}Recommended actions:${NC}"
        echo "1. Check service logs: docker-compose logs"
        echo "2. Restart problematic services: docker-compose restart <service>"
        echo "3. Check resource usage: docker stats"
        echo "4. Verify configuration files"
    fi
    
    return $overall_status
}

# Handle script arguments
case "${1:-all}" in
    "all")
        main
        ;;
    "containers")
        check_containers
        ;;
    "webapi")
        check_service "Web API" "$BASE_URL:$WEBAPI_PORT/health"
        check_webapi
        ;;
    "rag")
        check_service "RAG Service" "$BASE_URL:$RAG_PORT/health"
        check_rag_service
        ;;
    "qdrant")
        check_qdrant
        ;;
    "ollama")
        check_ollama
        ;;
    "resources")
        check_resources
        ;;
    "logs")
        check_logs
        ;;
    "performance")
        performance_test
        ;;
    *)
        echo "Usage: $0 {all|containers|webapi|rag|qdrant|ollama|resources|logs|performance}"
        echo "  all         - Run all health checks (default)"
        echo "  containers  - Check Docker containers"
        echo "  webapi      - Check Web API"
        echo "  rag         - Check RAG Service"
        echo "  qdrant      - Check Qdrant"
        echo "  ollama      - Check Ollama"
        echo "  resources   - Check system resources"
        echo "  logs        - Check service logs"
        echo "  performance - Run performance tests"
        exit 1
        ;;
esac
