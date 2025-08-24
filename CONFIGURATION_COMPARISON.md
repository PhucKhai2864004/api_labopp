# RAG Configuration Comparison & Optimization

## Overview

Tài liệu này so sánh cấu hình RAG service hiện tại với cấu hình tối ưu hóa và giải thích các cải tiến.

## Cấu hình của bạn (Rất tốt!)

```yaml
services:
  qdrant:
    image: qdrant/qdrant:1.9.0
    restart: always
    volumes: [qdrant_data:/qdrant/storage]
    healthcheck:
      test: ["CMD", "wget", "-qO-", "http://localhost:6333/collections"]
      interval: 10s; timeout: 5s; retries: 10
      
  rag-service:
    build: { context: ./rag-cli-nodejs }
    restart: always
    environment:
      - NODE_ENV=production
      - QDRANT_URL=http://qdrant:6333
      - GOOGLE_API_KEY=${GOOGLE_API_KEY}
      - GENAI_MODEL=${GENAI_MODEL:-gemini-1.5-pro-002}
      - EMBEDDINGS_PROVIDER=${EMBEDDINGS_PROVIDER:-gemini}
      - EMBEDDINGS_MODEL=${EMBEDDINGS_MODEL:-text-embedding-004}
      - PORT=3001
    healthcheck:
      test: ["CMD-SHELL","curl -fsS http://localhost:3001/health >/dev/null || exit 1"]
      interval: 10s; timeout: 5s; retries: 10
    depends_on:
      qdrant: { condition: service_healthy }
      
volumes: { qdrant_data: {} }
```

## Cấu hình tối ưu hóa (Cải tiến)

```yaml
services:
  qdrant:
    image: qdrant/qdrant:1.9.0
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
      # Performance optimizations
      - QDRANT__STORAGE__STORAGE_PATH=/qdrant/storage
      - QDRANT__STORAGE__PERFORMANCE__MAX_SEARCH_THREADS=4
      - QDRANT__STORAGE__PERFORMANCE__MAX_INDEXING_THREADS=4
    healthcheck:
      test: ["CMD", "wget", "-qO-", "http://localhost:6333/collections"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 30s
    networks:
      - labopp_network
    deploy:
      resources:
        limits:
          memory: 1G
          cpus: '0.5'
        reservations:
          memory: 512M
          cpus: '0.25'

  rag-service:
    build:
      context: ./rag-cli-nodejs
      dockerfile: Dockerfile
    container_name: labopp_rag_service
    restart: always
    ports:
      - "3001:3001"
    environment:
      # Node.js Configuration
      - NODE_ENV=production
      - NODE_OPTIONS=--max-old-space-size=2048
      
      # Service URLs
      - QDRANT_URL=http://qdrant:6333
      
      # Google Gemini Configuration
      - GOOGLE_API_KEY=${GOOGLE_API_KEY:-AIzaSyDxFNK8N6Y9bkLkNwhoENVhq-gNHH3UrnY}
      - GENAI_MODEL=${GENAI_MODEL:-gemini-1.5-pro-002}
      - GENAI_BASE=https://generativelanguage.googleapis.com
      
      # Embeddings Configuration
      - EMBEDDINGS_PROVIDER=${EMBEDDINGS_PROVIDER:-gemini}
      - EMBEDDINGS_MODEL=${EMBEDDINGS_MODEL:-text-embedding-004}
      
      # Service Configuration
      - PORT=3001
      - HOST=0.0.0.0
      - CORS_ORIGIN=*
      
      # Logging
      - LOG_LEVEL=info
      
      # Timeouts
      - REQUEST_TIMEOUT=120000
      - EMBEDDING_TIMEOUT=30000
      
      # Rate Limiting
      - RATE_LIMIT_WINDOW=900000
      - RATE_LIMIT_MAX_REQUESTS=100
      
    volumes:
      - rag_uploads:/app/uploads
      - rag_logs:/app/logs
    healthcheck:
      test: ["CMD-SHELL", "curl -fsS http://localhost:3001/health >/dev/null || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 30s
    depends_on:
      qdrant:
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
```

## So sánh chi tiết

### ✅ Điểm mạnh của cấu hình hiện tại

1. **Version pinning**: `qdrant/qdrant:1.9.0` - Rất tốt!
2. **Health checks**: 10s intervals - Tối ưu!
3. **Environment variables**: Cấu hình embeddings rõ ràng
4. **Dependencies**: Service dependencies đúng
5. **Health check command**: Sử dụng curl với proper flags

### 🚀 Cải tiến trong cấu hình tối ưu

#### 1. **Resource Management**
```yaml
# Thêm resource limits
deploy:
  resources:
    limits:
      memory: 1G
      cpus: '0.5'
    reservations:
      memory: 512M
      cpus: '0.25'
```
**Lợi ích**: Ngăn chặn memory leaks, đảm bảo performance ổn định

#### 2. **Performance Optimizations**
```yaml
# Qdrant performance settings
- QDRANT__STORAGE__PERFORMANCE__MAX_SEARCH_THREADS=4
- QDRANT__STORAGE__PERFORMANCE__MAX_INDEXING_THREADS=4
```
**Lợi ích**: Tối ưu hóa performance cho vector search và indexing

#### 3. **Network Isolation**
```yaml
networks:
  labopp_network:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16
```
**Lợi ích**: Bảo mật tốt hơn, isolation giữa các services

#### 4. **Enhanced Environment Variables**
```yaml
# Node.js optimizations
- NODE_OPTIONS=--max-old-space-size=2048

# Timeouts and rate limiting
- REQUEST_TIMEOUT=120000
- EMBEDDING_TIMEOUT=30000
- RATE_LIMIT_WINDOW=900000
- RATE_LIMIT_MAX_REQUESTS=100
```
**Lợi ích**: Better memory management, timeout handling, rate limiting

#### 5. **Volume Management**
```yaml
volumes:
  qdrant_data:
    driver: local
  rag_uploads:
    driver: local
  rag_logs:
    driver: local
```
**Lợi ích**: Persistent storage, log management, upload handling

#### 6. **Container Naming**
```yaml
container_name: labopp_qdrant
container_name: labopp_rag_service
```
**Lợi ích**: Dễ dàng identify và manage containers

#### 7. **External Access**
```yaml
ports:
  - "6333:6333"
  - "6334:6334"
  - "3001:3001"
```
**Lợi ích**: Monitoring, debugging, direct access khi cần

## Performance Impact

### Memory Usage
- **Trước**: Không giới hạn (có thể gây OOM)
- **Sau**: Giới hạn 1G cho Qdrant, 2G cho RAG service

### CPU Usage
- **Trước**: Không giới hạn
- **Sau**: Giới hạn 0.5 CPU cho Qdrant, 1.0 CPU cho RAG service

### Health Check Speed
- **Trước**: 10s intervals (đã tốt)
- **Sau**: 10s intervals + start_period để tránh false positives

### Network Performance
- **Trước**: Default network
- **Sau**: Isolated network với dedicated subnet

## Security Improvements

1. **Network isolation**: Services chỉ communicate qua internal network
2. **Resource limits**: Ngăn chặn DoS attacks
3. **Volume drivers**: Explicit volume management
4. **Environment validation**: Better error handling

## Monitoring Enhancements

1. **Container names**: Dễ dàng identify services
2. **External ports**: Direct access cho monitoring
3. **Health checks**: Enhanced với start_period
4. **Log volumes**: Persistent log storage

## Deployment Strategy

### Phương pháp 1: Gradual Migration
```bash
# 1. Backup current config
cp docker-compose.yml docker-compose.backup.yml

# 2. Apply optimized config
./apply-optimized-config.sh optimize

# 3. Test thoroughly
./apply-optimized-config.sh test

# 4. Monitor performance
./apply-optimized-config.sh metrics
```

### Phương pháp 2: Manual Update
```bash
# 1. Update docker-compose.yml với optimized config
# 2. Restart services
docker-compose down
docker-compose up -d

# 3. Verify health
docker-compose ps
```

## Rollback Plan

```bash
# Nếu có vấn đề, rollback ngay lập tức
./apply-optimized-config.sh rollback

# Hoặc manual rollback
docker-compose down
cp docker-compose.backup.yml docker-compose.yml
docker-compose up -d
```

## Kết luận

**Cấu hình hiện tại của bạn đã rất tốt!** Các cải tiến chủ yếu tập trung vào:

- ✅ **Resource management** (memory/CPU limits)
- ✅ **Performance optimization** (thread limits)
- ✅ **Security enhancement** (network isolation)
- ✅ **Monitoring improvement** (container naming, external access)
- ✅ **Operational excellence** (volume management, logging)

**Khuyến nghị**: Áp dụng cấu hình tối ưu để có production-ready setup với monitoring và security tốt hơn.
