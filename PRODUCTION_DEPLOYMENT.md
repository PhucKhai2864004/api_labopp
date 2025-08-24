# LabAssistant OPP Production Deployment Guide

## Overview

This guide provides step-by-step instructions for deploying the LabAssistant OPP system with RAG (Retrieval-Augmented Generation) services to production.

## Architecture

The production deployment includes:

- **Web API** (ASP.NET Core 8.0): Main application server
- **RAG Service** (Node.js): AI-powered document processing and test case generation
- **Qdrant**: Vector database for storing embeddings
- **Ollama**: Local LLM for embeddings and code review
- **Java Sandbox**: Code execution environment
- **Redis**: Caching and message queue
- **SQL Server**: Main database

## Prerequisites

### Server Requirements

- **OS**: Ubuntu 20.04+ / CentOS 8+ / RHEL 8+
- **CPU**: 8+ cores (16+ recommended)
- **RAM**: 16GB+ (32GB recommended)
- **Storage**: 100GB+ SSD
- **Network**: Stable internet connection

### Software Requirements

- Docker 20.10+
- Docker Compose 2.0+
- Git
- curl
- wget

### API Keys and Configuration

- Google Gemini API Key
- Google OAuth Client ID
- Database credentials
- Redis credentials

## Quick Start

### 1. Clone and Setup

```bash
# Clone the repository
git clone <repository-url>
cd api_labopp

# Make deployment script executable
chmod +x deploy-production.sh

# Copy environment template
cp env.production.template .env.production
```

### 2. Configure Environment

Edit `.env.production` with your production values:

```bash
# Database Configuration
DB_HOST=your-db-host
DB_PASSWORD=your-secure-password

# Redis Configuration
REDIS_HOST=your-redis-host
REDIS_PASSWORD=your-redis-password

# JWT Configuration
JWT_SECRET=your-super-secure-jwt-secret-key-here

# Google OAuth
GOOGLE_CLIENT_ID=your-google-client-id

# AI Services Configuration
GOOGLE_API_KEY=your-gemini-api-key
```

### 3. Deploy

```bash
# Full deployment
./deploy-production.sh deploy

# Or step by step
./deploy-production.sh stop
./deploy-production.sh start
./deploy-production.sh health
```

## Detailed Configuration

### Docker Compose Configuration

The `docker-compose.yml` includes:

- **Resource Limits**: Memory and CPU constraints for each service
- **Health Checks**: Automated health monitoring
- **Networking**: Isolated network with custom subnet
- **Volumes**: Persistent data storage
- **Environment Variables**: Production configuration

### Service Configuration

#### Web API
- Port: 5000
- Memory: 2GB limit
- Health check: `/health` endpoint

#### RAG Service
- Port: 3001
- Memory: 2GB limit
- CPU: 1.0 core limit
- Health check: `/health` endpoint

#### Qdrant
- Ports: 6333 (HTTP), 6334 (gRPC)
- Memory: 1GB limit
- Health check: Collections endpoint

#### Ollama
- Port: 11434
- Memory: 4GB limit
- Health check: Models endpoint

### Security Considerations

#### Network Security
- Use firewall rules to restrict external access
- Only expose necessary ports
- Consider using reverse proxy (nginx/traefik)

#### SSL/TLS
- Configure SSL certificates for HTTPS
- Use Let's Encrypt for free certificates
- Enable HSTS headers

#### Authentication
- Use strong JWT secrets
- Implement rate limiting
- Enable CORS properly

#### Container Security
- Run containers as non-root users
- Use multi-stage builds
- Scan images for vulnerabilities

## Monitoring and Logging

### Health Monitoring

```bash
# Check service health
./deploy-production.sh health

# View service logs
./deploy-production.sh logs

# Check service status
./deploy-production.sh status
```

### Log Management

- Logs are stored in Docker volumes
- Consider using ELK stack or similar
- Implement log rotation
- Monitor error rates

### Performance Monitoring

- Monitor CPU and memory usage
- Track API response times
- Monitor database performance
- Watch for memory leaks

## Backup and Recovery

### Automated Backups

```bash
# Create backup
./deploy-production.sh backup
```

### Manual Backup

```bash
# Backup Qdrant data
docker run --rm -v labopp_qdrant_data:/data -v $(pwd)/backup:/backup \
  alpine tar czf /backup/qdrant_data.tar.gz -C /data .

# Backup Ollama models
docker run --rm -v labopp_ollama_data:/data -v $(pwd)/backup:/backup \
  alpine tar czf /backup/ollama_data.tar.gz -C /data .
```

### Recovery

```bash
# Restore Qdrant data
docker run --rm -v labopp_qdrant_data:/data -v $(pwd)/backup:/backup \
  alpine tar xzf /backup/qdrant_data.tar.gz -C /data

# Restore Ollama models
docker run --rm -v labopp_ollama_data:/data -v $(pwd)/backup:/backup \
  alpine tar xzf /backup/ollama_data.tar.gz -C /data
```

## Troubleshooting

### Common Issues

#### Service Won't Start
```bash
# Check logs
docker-compose logs <service-name>

# Check resource usage
docker stats

# Restart service
docker-compose restart <service-name>
```

#### Memory Issues
```bash
# Check memory usage
docker stats

# Increase memory limits in docker-compose.yml
# Restart services
./deploy-production.sh restart
```

#### Network Issues
```bash
# Check network connectivity
docker network ls
docker network inspect labopp_labopp_network

# Restart network
docker-compose down
docker-compose up -d
```

#### Model Loading Issues
```bash
# Check Ollama status
curl http://localhost:11434/api/tags

# Pull models manually
curl -X POST http://localhost:11434/api/pull -d '{"name": "nomic-embed-text"}'
curl -X POST http://localhost:11434/api/pull -d '{"name": "llama3.2:3b"}'
```

### Performance Optimization

#### Database Optimization
- Use connection pooling
- Optimize queries
- Monitor slow queries
- Consider read replicas

#### Caching Strategy
- Use Redis for session storage
- Cache frequently accessed data
- Implement cache invalidation
- Monitor cache hit rates

#### Resource Optimization
- Monitor resource usage
- Adjust limits based on usage
- Use resource reservations
- Consider auto-scaling

## Maintenance

### Regular Maintenance Tasks

#### Daily
- Check service health
- Monitor error logs
- Check resource usage
- Verify backups

#### Weekly
- Update dependencies
- Review security patches
- Analyze performance metrics
- Clean up old logs

#### Monthly
- Full system backup
- Security audit
- Performance review
- Capacity planning

### Updates and Upgrades

```bash
# Pull latest changes
git pull origin main

# Rebuild and restart
./deploy-production.sh stop
./deploy-production.sh build
./deploy-production.sh start
```

## Support

### Getting Help

1. Check the logs: `./deploy-production.sh logs`
2. Verify configuration: `./deploy-production.sh health`
3. Review this documentation
4. Check GitHub issues
5. Contact support team

### Emergency Procedures

#### Service Outage
1. Check service status: `./deploy-production.sh status`
2. Restart services: `./deploy-production.sh restart`
3. Check resource usage: `docker stats`
4. Review recent changes

#### Data Loss
1. Stop all services: `./deploy-production.sh stop`
2. Restore from backup
3. Verify data integrity
4. Restart services: `./deploy-production.sh start`

## Additional Resources

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [Qdrant Documentation](https://qdrant.tech/documentation/)
- [Ollama Documentation](https://ollama.ai/docs)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)

## License

This deployment guide is part of the LabAssistant OPP project and follows the same license terms.
