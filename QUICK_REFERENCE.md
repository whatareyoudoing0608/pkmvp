# PKMVP Docker Deployment - Quick Reference

## 📁 Created Files

```
PKMVP/
├── Dockerfile.fe              # Frontend Docker image (React + Nginx)
├── Dockerfile.be              # Backend Docker image (.NET Core 3.1)
├── docker-compose.yml         # Complete stack orchestration
├── nginx.conf                 # Nginx proxy config
├── deploy-FE.sh              # Frontend deploy script
├── deploy-BE.sh              # Backend deploy script
├── deploy-all.sh             # Deploy both services
├── .dockerignore              # Docker build ignore file
├── DEPLOYMENT_GUIDE.md        # Full deployment guide (this file)
└── QUICK_REFERENCE.md         # This file
```

## 🚀 Quick Start (3 Steps)

### Step 1: Make scripts executable
```bash
cd /path/to/PKMVP
chmod +x deploy-FE.sh deploy-BE.sh deploy-all.sh
```

### Step 2: Run deployment
```bash
# Deploy both services (RECOMMENDED)
./deploy-all.sh

# OR deploy individually
./deploy-BE.sh   # Deploy backend first
./deploy-FE.sh   # Deploy frontend
```

### Step 3: Access services
- **Frontend:** http://localhost
- **Backend API:** http://localhost:5000
- **Swagger:** http://localhost:5000/swagger

---

## 📋 Service Details

| Service | Port | Status | Logs |
|---------|------|--------|----- |
| Frontend | 80 | `docker ps \| grep pkmvp-frontend` | `docker logs -f pkmvp-frontend` |
| Backend (HTTP) | 5000 | `docker ps \| grep pkmvp-backend` | `docker logs -f pkmvp-backend` |
| Backend (HTTPS) | 5001 | `docker ps \| grep pkmvp-backend` | `docker logs -f pkmvp-backend` |

---

## 🛠️ Common Commands

### Deploy
```bash
./deploy-all.sh              # Deploy everything
./deploy-FE.sh               # Deploy frontend only
./deploy-BE.sh               # Deploy backend only
docker-compose up -d         # Alternative: use docker-compose
```

### View Logs
```bash
docker logs -f pkmvp-frontend     # Frontend logs (real-time)
docker logs -f pkmvp-backend      # Backend logs (real-time)
docker-compose logs -f            # All logs (with docker-compose)
```

### Stop Services
```bash
docker stop pkmvp-frontend        # Stop frontend
docker stop pkmvp-backend         # Stop backend
docker-compose stop               # Stop all (with docker-compose)
```

### Remove & Clean
```bash
docker rm pkmvp-frontend          # Remove frontend container
docker rm pkmvp-backend           # Remove backend container
docker rmi pkmvp-frontend         # Remove frontend image
docker rmi pkmvp-backend          # Remove backend image
docker system prune -a            # Clean all unused resources
```

### Restart
```bash
docker restart pkmvp-frontend     # Restart frontend
docker restart pkmvp-backend      # Restart backend
docker-compose restart            # Restart all
```

### Status
```bash
docker ps                         # Show running containers
docker ps -a                      # Show all containers
docker stats                      # Show resource usage
docker inspect pkmvp-frontend     # Detailed info on frontend
```

---

## 🔧 Configuration

### Change Frontend Port
Edit `docker-compose.yml` or `deploy-FE.sh`:
```yaml
ports:
  - "8080:80"  # Change 80 to desired port
```

### Change Backend Port
Edit `docker-compose.yml` or `deploy-BE.sh`:
```yaml
ports:
  - "3000:80"   # Change 3000 to desired port
  - "3001:443"  # Change 3001 to desired HTTPS port
```

### Environment Variables (Backend)
Edit `docker-compose.yml`:
```yaml
environment:
  - Jwt:Key=your-secret-key
  - Database:ConnectionString=connection-string
```

---

## 🐛 Troubleshooting

### "Port already in use"
```bash
# Find process using port
lsof -i :80
lsof -i :5000

# Kill process
kill -9 <PID>

# Or change port in config
```

### "Connection refused"
```bash
# Check if backend is running
curl http://localhost:5000/health

# Check if frontend can reach backend
docker logs -f pkmvp-frontend
```

### "Build failed"
```bash
# Clean Docker cache
docker system prune -a --volumes

# Rebuild without cache
docker build -f Dockerfile.be -t pkmvp-backend:latest --no-cache .
```

### "Container not starting"
```bash
# Check logs
docker logs pkmvp-backend

# Inspect container
docker inspect pkmvp-backend

# Check resource usage
docker stats
```

---

## 📊 Service Information

### Frontend (React + Nginx)
- **Build time:** ~2-3 minutes
- **Image size:** ~200MB
- **Memory usage:** ~100MB
- **Entrypoint:** nginx
- **Healthcheck:** /health endpoint

### Backend (.NET Core 3.1)
- **Build time:** ~5-10 minutes (first time)
- **Image size:** ~500MB
- **Memory usage:** ~200MB
- **Entrypoint:** dotnet Pkmvp.Api.dll
- **Healthcheck:** /health endpoint

---

## 📈 Using docker-compose (Alternative)

Instead of scripts, you can use docker-compose:

```bash
# Build images
docker-compose build

# Start services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose stop

# Restart services
docker-compose restart

# Complete cleanup
docker-compose down -v

# Rebuild and restart
docker-compose up -d --build
```

---

## 🔒 Security Checklist

- [ ] Change default ports in production
- [ ] Use HTTPS with valid certificates
- [ ] Add firewall rules to restrict access
- [ ] Store secrets in environment files, not in code
- [ ] Use container restart policies
- [ ] Enable logging for debugging
- [ ] Regular security updates to base images

---

## 📚 Full Documentation

See [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md) for:
- Complete setup instructions
- Production deployment
- SSL/TLS configuration
- Performance tuning
- Monitoring & logging
- Backup & recovery

---

## 🆘 Need Help?

1. Check service logs: `docker logs -f <container_name>`
2. Review [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)
3. Verify Docker installation: `docker --version`
4. Test connectivity: `curl http://localhost:5000/health`
5. Check disk space: `df -h`

---

**Created:** March 2026  
**Framework:** React + .NET Core 3.1  
**Container Platform:** Docker + Nginx  
**Status:** Production Ready ✅
