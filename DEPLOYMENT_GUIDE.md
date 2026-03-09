# PKMVP Docker Deployment Guide

## Overview
This guide covers deploying PKMVP Frontend (React) and Backend (.NET Core) using Docker with Nginx reverse proxy.

## Prerequisites

- Linux server (Ubuntu 20.04+ recommended)
- Docker installed (`docker --version`)
- Docker Compose installed (`docker-compose --version`)
- Git (for cloning repository)
- Bash shell

### Install Docker on Linux

```bash
# For Ubuntu/Debian
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Add your user to docker group (optional, to avoid sudo)
sudo usermod -aG docker $USER
newgrp docker
```

## Project Structure

```
PKMVP/
├── Dockerfile.fe          # Frontend image definition
├── Dockerfile.be          # Backend image definition
├── docker-compose.yml     # Orchestration file
├── nginx.conf            # Nginx configuration
├── deploy-FE.sh          # Frontend deployment script
├── deploy-BE.sh          # Backend deployment script
├── PKMVP/
│   └── Pkmvp.Api/        # .NET Core API source
└── PKMVP-FE/
    └── employee-manager-fe/  # React app source
```

## Quick Start

### Deploy Both Services (Recommended)

```bash
# Navigate to project root
cd /path/to/PKMVP

# Make scripts executable
chmod +x deploy-FE.sh deploy-BE.sh
chmod +x deploy-all.sh  # If using combined script

# Deploy everything
./deploy-all.sh
```

### Deploy Frontend Only

```bash
chmod +x deploy-FE.sh
./deploy-FE.sh
```

**Expected output:**
```
========================================
PKMVP Frontend Deployment
========================================

[INFO] Checking Docker installation...
[INFO] Docker found: Docker version 24.0.0
[INFO] Building Docker image: pkmvp-frontend:latest
...
[INFO] ✓ Container started successfully
[INFO] Frontend is now running!
[INFO] URL: http://localhost
```

### Deploy Backend Only

```bash
chmod +x deploy-BE.sh
./deploy-BE.sh
```

**Expected output:**
```
========================================
PKMVP Backend Deployment
========================================

[INFO] Building Docker image: pkmvp-backend:latest
...
[INFO] ✓ Container is running
[INFO] Backend API is now running!
[INFO] API URL: http://localhost:5000
[INFO] Swagger: http://localhost:5000/swagger
```

## Access Services

After deployment, access:

- **Frontend:** http://localhost (Port 80)
- **Backend API:** http://localhost:5000 (Port 5000)
- **Backend Swagger:** http://localhost:5000/swagger (Port 5000)
- **Backend HTTPS:** https://localhost:5001 (Port 5001)

## Docker Compose Alternative

Instead of individual scripts, you can use docker-compose for easier management:

```bash
# Build and start both services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down

# Rebuild images
docker-compose up -d --build
```

## Container Management

### View Logs

```bash
# Frontend
docker logs -f pkmvp-frontend

# Backend
docker logs -f pkmvp-backend

# Combined (with docker-compose)
docker-compose logs -f
```

### Stop Services

```bash
# Stop frontend
docker stop pkmvp-frontend

# Stop backend
docker stop pkmvp-backend

# Stop all (with docker-compose)
docker-compose stop
```

### Remove Containers

```bash
# Remove frontend
docker rm pkmvp-frontend

# Remove backend
docker rm pkmvp-backend

# Remove all (with docker-compose)
docker-compose down
```

### Restart Services

```bash
# Frontend
docker restart pkmvp-frontend

# Backend
docker restart pkmvp-backend

# All (with docker-compose)
docker-compose restart
```

## Configuration

### Environment Variables

#### Backend (.NET API)

Edit `docker-compose.yml` or in `deploy-BE.sh`:

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - ASPNETCORE_URLS=http://+:80
  - Jwt:Key=your-secret-key-here
  - Database:ConnectionString=your-connection-string
```

#### Frontend (React)

Environment variables can be passed during build or in nginx config.

### Port Configuration

Edit these files to change ports:

**Dockerfile.fe:**
```dockerfile
EXPOSE 80  # Change to desired port
```

**Dockerfile.be:**
```dockerfile
EXPOSE 80  # HTTP
EXPOSE 443   # HTTPS
```

**docker-compose.yml:**
```yaml
services:
  frontend:
    ports:
      - "80:80"  # Change first port to desired
  backend:
    ports:
      - "5000:80"    # Change first port to desired
      - "5001:443"   # HTTPS
```

### Nginx Configuration

Edit `nginx.conf` to customize:
- Cache settings
- Gzip compression
- API proxy rules
- Security headers

## Troubleshooting

### Container Won't Start

```bash
# Check logs
docker logs pkmvp-frontend
docker logs pkmvp-backend

# Inspect container
docker inspect pkmvp-frontend
docker inspect pkmvp-backend
```

### Port Already in Use

```bash
# Find process using port
lsof -i :80
lsof -i :5000

# Kill process
kill -9 <PID>

# Or change port in docker-compose.yml or deploy scripts
```

### Build Fails

```bash
# Clean up Docker resources
docker system prune -a

# Rebuild without cache
docker-compose up -d --build --no-cache
```

### API Connection Issues

Ensure backend is running before frontend:
```bash
# Check if backend is responding
curl http://localhost:5000/health

# Check network
docker inspect pkmvp-network
```

## Production Deployment

### Best Practices

1. **Use specific image tags** instead of `latest`:
   ```bash
   docker build -t pkmvp-frontend:v1.0.0 -f Dockerfile.fe .
   ```

2. **Set resource limits** in docker-compose.yml:
   ```yaml
   services:
     frontend:
       deploy:
         resources:
           limits:
             cpus: '1'
             memory: 512M
   ```

3. **Use environment files**:
   ```bash
   docker run -d --env-file .env.production ...
   ```

4. **Enable logging drivers**:
   ```yaml
   services:
     backend:
       logging:
         driver: "json-file"
         options:
           max-size: "10m"
           max-file: "3"
   ```

5. **Use health checks**:
   ```yaml
   services:
     backend:
       healthcheck:
         test: ["CMD", "curl", "-f", "http://localhost/health"]
         interval: 30s
         timeout: 10s
         retries: 3
   ```

### Domain Configuration

For custom domain (e.g., `api.example.com`):

1. Update `nginx.conf`:
   ```nginx
   server_name api.example.com;
   ```

2. Add SSL certificate (Let's Encrypt):
   ```bash
   docker run -it --rm --name certbot \
     -v /etc/letsencrypt:/etc/letsencrypt \
     certbot/certbot certonly --standalone -d api.example.com
   ```

## Monitoring

### Container Stats

```bash
docker stats pkmvp-frontend pkmvp-backend
```

### Health Monitoring

```bash
# Check every 30 seconds
watch -n 30 'docker ps --format "table {{.Names}}\t{{.Status}}"'
```

## Backup & Recovery

### Backup Data

```bash
# Backup logs
docker logs pkmvp-backend > backend-logs.txt
docker logs pkmvp-frontend > frontend-logs.txt
```

### Update Services

```bash
# Pull latest code
git pull

# Rebuild and restart
docker-compose up -d --build
```

## Security Notes

1. ✅ Never expose database connection strings in Docker images
2. ✅ Use environment files for sensitive configuration
3. ✅ Restrict port access with firewall
4. ✅ Use HTTPS in production
5. ✅ Keep Docker and base images updated

## Support & Troubleshooting

For issues, check:

1. Docker logs: `docker logs <container_name>`
2. Network: `docker network inspect pkmvp-network`
3. Environment: `docker inspect <container_name>`
4. Resource usage: `docker stats`

---

**Last Updated:** March 2026
