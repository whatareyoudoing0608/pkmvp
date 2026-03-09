# PKMVP Docker Deployment Files - Complete Index

## 📚 Overview

This directory contains everything needed to deploy PKMVP (Frontend React app + Backend .NET API) using Docker on Linux with Nginx.

---

## 📦 Docker Files

### 1. **Dockerfile.fe** - Frontend Image
- **Purpose:** Build React application with Vite and serve with Nginx
- **Base Image:** `node:20-alpine` (build) + `nginx:alpine` (runtime)
- **Size:** ~200MB
- **Port:** 80
- **Build Time:** ~2-3 minutes

```bash
# Use this to build frontend manually
docker build -f Dockerfile.fe -t pkmvp-frontend:latest .
```

### 2. **Dockerfile.be** - Backend Image
- **Purpose:** Build .NET Core 3.1 API and run with ASP.NET runtime
- **Base Image:** `mcr.microsoft.com/dotnet/sdk:3.1` (build) + `mcr.microsoft.com/dotnet/aspnet:3.1` (runtime)
- **Size:** ~500MB
- **Port:** 80 (HTTP), 443 (HTTPS)
- **Build Time:** ~5-10 minutes (first time)

```bash
# Use this to build backend manually
docker build -f Dockerfile.be -t pkmvp-backend:latest .
```

### 3. **docker-compose.yml** - Orchestration File
- **Purpose:** Define and run both services with a single command
- **Services:**
  - `frontend` - React app on port 80
  - `backend` - .NET API on port 5000
- **Network:** `pkmvp-network` (automatic interconnection)
- **Restart Policy:** `unless-stopped` (auto-restart on failure)

```bash
# Deploy with docker-compose
docker-compose up -d
docker-compose stop
docker-compose down
```

### 4. **nginx.conf** - Nginx Configuration
- **Purpose:** Configure Nginx reverse proxy for frontend + backend routing
- **Features:**
  - Gzip compression for static assets
  - Cache control for performance
  - API proxy to backend: `/api/` → `http://backend:80`
  - React Router fallback to `index.html`
  - Health endpoint for monitoring

```nginx
# Key sections:
# - Static asset caching (365 days)
# - API routing to backend
# - React SPA routing support
```

### 5. **.dockerignore** - Docker Build Ignore
- **Purpose:** Exclude unnecessary files from Docker build context
- **Ignores:**
  - `node_modules/`, `dist/`, `bin/`, `obj/`
  - `.git/`, `.github/`, `.vs/`
  - `*.log`, `.env` files
  - IDE configs, OS files

---

## 🚀 Deployment Scripts

### 6. **deploy-FE.sh** - Frontend Deployment Script
```bash
#!/bin/bash
# Purpose: One-command frontend deployment
# Features:
#   - Checks Docker installation
#   - Cleans up existing containers
#   - Builds image with error handling
#   - Starts container with monitoring
#   - Displays deployment info

# Usage:
chmod +x deploy-FE.sh
./deploy-FE.sh

# Output:
# ========================================
# PKMVP Frontend Deployment
# ========================================
# ✓ Container started successfully
# Frontend is now running!
# URL: http://localhost
```

**What it does:**
1. Validates Docker is installed
2. Stops & removes existing frontend container
3. Builds `pkmvp-frontend:latest` image
4. Runs container on port 80
5. Verifies container is healthy
6. Shows access URLs and logs commands

---

### 7. **deploy-BE.sh** - Backend Deployment Script
```bash
#!/bin/bash
# Purpose: One-command backend deployment
# Features:
#   - Checks Docker installation
#   - Cleans up existing containers
#   - Builds .NET image with compilation
#   - Starts container with monitoring
#   - Tests API health endpoint
#   - Displays Swagger URL

# Usage:
chmod +x deploy-BE.sh
./deploy-BE.sh

# Output:
# ========================================
# PKMVP Backend Deployment
# ========================================
# ✓ Container is running
# Backend API is now running!
# API URL: http://localhost:5000
# Swagger: http://localhost:5000/swagger
```

**What it does:**
1. Validates Docker is installed
2. Stops & removes existing backend container
3. Builds `pkmvp-backend:latest` image (.NET compilation)
4. Runs container on ports 5000/5001
5. Waits for API to start
6. Tests health endpoint
7. Shows API URLs and logs commands

---

### 8. **deploy-all.sh** - Complete Stack Deployment
```bash
#!/bin/bash
# Purpose: One-command deployment of FULL STACK (Frontend + Backend)
# Features:
#   - Colored output with progress indicators
#   - Builds both images sequentially
#   - Starts backend first (dependency)
#   - Starts frontend second
#   - Comprehensive health checks
#   - Beautiful summary output

# Usage:
chmod +x deploy-all.sh
./deploy-all.sh

# Output:
# ╔════════════════════════════════════╗
# ║  ✓ Deployment Complete!            ║
# ╚════════════════════════════════════╝
# Services Information:
#   Frontend: http://localhost
#   Backend: http://localhost:5000
#   Swagger: http://localhost:5000/swagger
```

**What it does:**
1. Validates prerequisites
2. Cleans up existing containers
3. Builds backend image
4. Builds frontend image
5. Starts backend container
6. Starts frontend container
7. Verifies both are running
8. Displays comprehensive summary with useful commands

---

## 📖 Documentation Files

### 9. **DEPLOYMENT_GUIDE.md** - Complete Documentation
- **Pages:** ~500+ lines of comprehensive documentation
- **Sections:**
  - Prerequisites & installation
  - Quick start guide
  - Docker Compose usage
  - Container management
  - Configuration options
  - Port customization
  - Troubleshooting guide
  - Production deployment best practices
  - SSL/TLS setup
  - Monitoring & health checks
  - Backup & recovery
  - Security recommendations

**Use this when you need:**
- Detailed setup instructions
- Production deployment guidance
- Performance optimization tips
- Certificate management
- Monitoring setup
- Security hardening

---

### 10. **QUICK_REFERENCE.md** - Quick Start Guide
- **Pages:** ~200 lines of quick reference
- **Sections:**
  - File overview
  - 3-step quick start
  - Common commands
  - Configuration changes
  - Troubleshooting
  - Service information
  - Security checklist

**Use this for:**
- Quick lookups
- Common command examples
- Fast troubleshooting
- Port configuration
- Status checking

---

### 11. DATABASE_MIGRATIONS.md** - Database Setup
- Database migration scripts (if needed)
- Schema initialization
- Seed data
- Backup procedures

---

## ⚙️ Configuration Files

### 12. **.env.example** - Environment Variables Template
```bash
# Key configurations:
ASPNETCORE_ENVIRONMENT=Production
Jwt__Key=your-secret-key
Database__ConnectionString=your-connection-string
VITE_API_BASE_URL=http://localhost:5000

# Usage:
# 1. Copy to .env
# 2. Fill in your values
# 3. Source before deployment
```

---

## 📋 File Organization

```
PKMVP/
│
├─ 🐳 Docker Files
│  ├─ Dockerfile.fe            # Frontend image
│  ├─ Dockerfile.be            # Backend image
│  ├─ docker-compose.yml       # Orchestration
│  ├─ nginx.conf              # Nginx config
│  └─ .dockerignore           # Build ignore
│
├─ 🚀 Deployment Scripts
│  ├─ deploy-FE.sh            # Frontend deploy
│  ├─ deploy-BE.sh            # Backend deploy
│  └─ deploy-all.sh           # Full stack deploy
│
├─ 📖 Documentation
│  ├─ DEPLOYMENT_GUIDE.md     # Complete guide
│  ├─ QUICK_REFERENCE.md      # Quick commands
│  └─ README.md               # This file
│
├─ ⚙️ Configuration
│  ├─ .env.example            # Env template
│  └─ appsettings.json        # .NET config
│
├─ 📁 Source Code
│  ├─ PKMVP/                  # Backend source
│  │  └─ Pkmvp.Api/
│  │
│  └─ PKMVP-FE/               # Frontend source
│     └─ employee-manager-fe/
│
└─ 📚 Additional
   └─ db/                     # Database files
      └─ migrations/
```

---

## 🎯 Quick Start Paths

### Path 1: Super Fast (3 commands)
```bash
chmod +x deploy-all.sh
./deploy-all.sh
# Done! Everything is running
```

### Path 2: Manual Control
```bash
chmod +x deploy-BE.sh deploy-FE.sh
./deploy-BE.sh   # Start backend
./deploy-FE.sh   # Start frontend
```

### Path 3: Using docker-compose
```bash
docker-compose up -d
# All services running together
```

---

## 🔍 File Purposes at a Glance

| File | Type | Purpose | Size |
|------|------|---------|------|
| Dockerfile.fe | Docker | Build React image | - |
| Dockerfile.be | Docker | Build .NET API image | - |
| docker-compose.yml | Config | Orchestrate containers | 25 lines |
| nginx.conf | Config | Proxy & serve frontend | 40 lines |
| .dockerignore | Config | Build optimization | 25 lines |
| deploy-FE.sh | Script | Deploy frontend | 150 lines |
| deploy-BE.sh | Script | Deploy backend | 180 lines |
| deploy-all.sh | Script | Deploy everything | 280 lines |
| DEPLOYMENT_GUIDE.md | Docs | Complete guide | 500+ lines |
| QUICK_REFERENCE.md | Docs | Quick lookups | 200+ lines |
| .env.example | Config | Environment template | 30 lines |

---

## ✅ Verification Checklist

After deployment, verify:

- [ ] Frontend container running: `docker ps | grep pkmvp-frontend`
- [ ] Backend container running: `docker ps | grep pkmvp-backend`
- [ ] Frontend accessible: `curl http://localhost`
- [ ] Backend API works: `curl http://localhost:5000/health`
- [ ] Swagger available: `curl http://localhost:5000/swagger`
- [ ] Logs show no errors: `docker logs pkmvp-backend`

---

## 🆘 Help & Support

1. **Quick questions:** Check [QUICK_REFERENCE.md](QUICK_REFERENCE.md)
2. **Setup issues:** See [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md#troubleshooting)
3. **Command help:** Run `./deploy-all.sh --help` (if added) or check scripts
4. **Docker help:** `docker --help` or `docker-compose --help`

---

## 📝 Next Steps

1. **First time deployment:**
   ```bash
   chmod +x deploy-all.sh
   ./deploy-all.sh
   ```

2. **Access services:**
   - Frontend: http://localhost/
   - Backend: http://localhost:5000
   - Swagger: http://localhost:5000/swagger

3. **Monitor:**
   ```bash
   docker logs -f pkmvp-backend
   docker logs -f pkmvp-frontend
   ```

4. **Production deployment:**
   - See [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md#production-deployment)
   - Configure SSL/TLS
   - Set up monitoring
   - Configure backups

---

**Created:** March 2026  
**Status:** ✅ Production Ready  
**Docker Version:** 20.10+  
**OS:** Linux (Ubuntu 20.04+ recommended)
