# Deploy RAG Service to Existing Droplet

## Overview

Hướng dẫn này sẽ giúp bạn thêm RAG (Retrieval-Augmented Generation) services vào droplet LabAssistant OPP hiện tại một cách an toàn và hiệu quả.

## Phương pháp được khuyến nghị

**Có, phương pháp này rất phù hợp!** Vì:

✅ **Tương thích tốt**: Docker Compose cho phép thêm service mới mà không ảnh hưởng đến hệ thống hiện có  
✅ **Isolation**: Mỗi service chạy độc lập trong container riêng  
✅ **Rollback dễ dàng**: Có thể rollback nhanh chóng nếu có vấn đề  
✅ **Resource control**: Kiểm soát được tài nguyên cho từng service  
✅ **Zero downtime**: Có thể deploy mà không làm gián đoạn service hiện tại  

## Yêu cầu hệ thống

### Tài nguyên tối thiểu
- **RAM**: +4GB (cho RAG services)
- **Storage**: +20GB (cho models và data)
- **CPU**: +2 cores

### Ports cần mở
- `3001`: RAG Service
- `6333`: Qdrant HTTP
- `6334`: Qdrant gRPC  
- `11434`: Ollama

## Các bước thực hiện

### 1. Chuẩn bị

```bash
# SSH vào droplet
ssh root@your-droplet-ip

# Di chuyển đến thư mục project
cd /path/to/your/labopp-project

# Tạo backup
cp docker-compose.yml docker-compose.backup.yml
```

### 2. Kiểm tra tài nguyên hiện tại

```bash
# Kiểm tra RAM
free -h

# Kiểm tra disk space
df -h

# Kiểm tra CPU
nproc

# Kiểm tra ports đang sử dụng
netstat -tuln | grep -E ':(3001|6333|6334|11434)'
```

### 3. Deploy RAG Services

#### Phương pháp 1: Sử dụng script tự động (Khuyến nghị)

```bash
# Tải script deployment
wget https://raw.githubusercontent.com/your-repo/api_labopp/main/deploy-rag-to-droplet.sh

# Cấp quyền thực thi
chmod +x deploy-rag-to-droplet.sh

# Chạy deployment
./deploy-rag-to-droplet.sh deploy
```

#### Phương pháp 2: Deploy thủ công

```bash
# 1. Cập nhật docker-compose.yml
# Thêm RAG services vào file docker-compose.yml hiện tại

# 2. Pull images
docker-compose pull qdrant ollama

# 3. Build RAG service
docker-compose build rag-service

# 4. Start RAG services
docker-compose up -d qdrant ollama rag-service

# 5. Kiểm tra status
docker-compose ps
```

### 4. Cấu hình môi trường

```bash
# Thêm environment variables cho RAG
export GOOGLE_API_KEY="your-gemini-api-key"
export GENAI_MODEL="gemini-1.5-flash-latest"

# Hoặc thêm vào .env file
echo "GOOGLE_API_KEY=your-gemini-api-key" >> .env
echo "GENAI_MODEL=gemini-1.5-flash-latest" >> .env
```

### 5. Khởi tạo models

```bash
# Đợi Ollama khởi động
sleep 60

# Pull required models
curl -X POST http://localhost:11434/api/pull -d '{"name": "nomic-embed-text"}'
curl -X POST http://localhost:11434/api/pull -d '{"name": "llama3.2:3b"}'
```

### 6. Kiểm tra và test

```bash
# Kiểm tra health
./deploy-rag-to-droplet.sh check

# Test integration
./deploy-rag-to-droplet.sh test

# Kiểm tra logs
docker-compose logs rag-service
```

## Monitoring và Maintenance

### Kiểm tra tài nguyên

```bash
# Monitor resource usage
docker stats

# Check specific service
docker stats labopp_rag_service labopp_qdrant labopp_ollama
```

### Logs monitoring

```bash
# View all logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f rag-service
docker-compose logs -f qdrant
docker-compose logs -f ollama
```

### Backup và Recovery

```bash
# Backup RAG data
docker run --rm -v labopp_qdrant_data:/data -v $(pwd)/backup:/backup \
  alpine tar czf /backup/qdrant_data.tar.gz -C /data .

docker run --rm -v labopp_ollama_data:/data -v $(pwd)/backup:/backup \
  alpine tar czf /backup/ollama_data.tar.gz -C /data .
```

## Troubleshooting

### Vấn đề thường gặp

#### 1. Port conflicts
```bash
# Kiểm tra ports đang sử dụng
netstat -tuln | grep -E ':(3001|6333|6334|11434)'

# Nếu có conflict, thay đổi ports trong docker-compose.yml
```

#### 2. Memory issues
```bash
# Kiểm tra memory usage
free -h
docker stats

# Tăng swap nếu cần
sudo fallocate -l 4G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile
```

#### 3. Model download issues
```bash
# Kiểm tra Ollama status
curl http://localhost:11434/api/tags

# Pull models manually
curl -X POST http://localhost:11434/api/pull -d '{"name": "nomic-embed-text"}'
```

#### 4. Service không start
```bash
# Kiểm tra logs
docker-compose logs rag-service

# Restart service
docker-compose restart rag-service

# Check dependencies
docker-compose ps
```

### Rollback nếu có vấn đề

```bash
# Rollback to previous configuration
./deploy-rag-to-droplet.sh rollback

# Hoặc manual rollback
docker-compose down
cp docker-compose.backup.yml docker-compose.yml
docker-compose up -d
```

## Performance Optimization

### Resource tuning

```bash
# Adjust memory limits in docker-compose.yml
deploy:
  resources:
    limits:
      memory: 2G  # Tăng nếu cần
      cpus: '1.0'
```

### Monitoring setup

```bash
# Install monitoring tools
apt-get update
apt-get install -y htop iotop

# Setup log rotation
cat > /etc/logrotate.d/docker-compose << EOF
/path/to/your/project/logs/*.log {
    daily
    rotate 7
    compress
    delaycompress
    missingok
    notifempty
}
EOF
```

## Security Considerations

### Firewall configuration

```bash
# Allow only necessary ports
ufw allow 3001/tcp  # RAG Service
ufw allow 6333/tcp  # Qdrant HTTP
ufw allow 6334/tcp  # Qdrant gRPC
ufw allow 11434/tcp # Ollama

# Or restrict to specific IPs
ufw allow from your-ip to any port 3001
```

### SSL/TLS setup

```bash
# Setup reverse proxy with SSL
# Sử dụng nginx hoặc traefik để proxy requests
```

## Testing sau khi deploy

### 1. Test RAG Service endpoints

```bash
# Health check
curl http://localhost:3001/health

# Test PDF ingest
curl -X POST http://localhost:3001/ingest \
  -F "pdfFile=@test.pdf" \
  -F "assignmentId=1"

# Test test case generation
curl -X POST http://localhost:3001/suggest-testcases \
  -H "Content-Type: application/json" \
  -d '{"assignmentId": "1"}'
```

### 2. Test Web API integration

```bash
# Test AI endpoints
curl http://localhost:5000/api/ai/suggest-testcases

# Test với authentication
curl -H "Authorization: Bearer your-token" \
  http://localhost:5000/api/ai/suggest-testcases
```

### 3. Performance testing

```bash
# Test response times
time curl http://localhost:3001/health
time curl http://localhost:5000/api/ai/suggest-testcases
```

## Maintenance Schedule

### Daily
- Kiểm tra service health
- Monitor resource usage
- Review error logs

### Weekly  
- Update dependencies
- Clean up old logs
- Backup data

### Monthly
- Security updates
- Performance review
- Full system backup

## Support và Troubleshooting

### Getting help

1. Check logs: `docker-compose logs rag-service`
2. Verify configuration: `./deploy-rag-to-droplet.sh check`
3. Test connectivity: `./deploy-rag-to-droplet.sh test`
4. Review this documentation
5. Contact support team

### Emergency procedures

#### Service outage
```bash
# Quick restart
docker-compose restart rag-service

# Full restart
docker-compose down && docker-compose up -d
```

#### Data corruption
```bash
# Restore from backup
docker-compose down
# Restore volumes from backup
docker-compose up -d
```

## Kết luận

Phương pháp deploy RAG service lên droplet hiện tại là **rất phù hợp** và được khuyến nghị vì:

- ✅ **An toàn**: Không ảnh hưởng đến hệ thống hiện tại
- ✅ **Linh hoạt**: Có thể scale và maintain dễ dàng  
- ✅ **Hiệu quả**: Tận dụng được infrastructure hiện có
- ✅ **Rollback**: Có thể rollback nhanh chóng nếu cần

Với hướng dẫn này, bạn có thể deploy RAG services một cách an toàn và hiệu quả lên droplet hiện tại.
