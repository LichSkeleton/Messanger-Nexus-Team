# NexusTeam MVP Installation - Complete Guide

## Table of Contents

1. [System Requirements](#system-requirements)
2. [Prerequisites](#prerequisites)
3. [Docker Installation](#docker-installation)
4. [Application Configuration](#application-configuration)
5. [Building and Running the Application](#building-and-running-the-application)
6. [Verification](#verification)
7. [Troubleshooting](#troubleshooting)
8. [Additional Information](#additional-information)

---

## System Requirements

### Required Components

- **Docker Desktop** (Windows/Mac) or **Docker Engine** + **Docker Compose** (Linux)
  - Minimum 8GB RAM allocated to Docker
  - Docker Compose v2.0 or later
- **.NET 8.0 SDK** or higher (for building and running the application)
- **Git** (for cloning the repository)

### Optional Tools

- **Visual Studio 2022** / **JetBrains Rider** / **VS Code** (for development)
- **MongoDB Shell (mongosh)** (for MongoDB management)
- **redis-cli** (for Redis management)
- **SQL*Plus** or **Oracle SQL Developer** (for Oracle management)

---

## Prerequisites

### 1. Install Docker

**Windows/Mac:**
- Download and install [Docker Desktop](https://www.docker.com/products/docker-desktop)
- Ensure Docker Desktop is running
- Allocate at least 8GB RAM to Docker (Settings → Resources → Memory)

**Linux:**
```bash
# Install Docker Engine and Docker Compose
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo apt-get install docker-compose-plugin
```

**Verify Installation:**
```bash
docker --version
docker-compose --version
```

### 2. Install .NET 8.0 SDK

**Download and install from:**
- [.NET 8.0 Download Page](https://dotnet.microsoft.com/download/dotnet/8.0)

**Verify Installation:**
```bash
dotnet --version
# Expected output: 8.0.x or higher
```

### 3. Clone the Repository

```bash
git clone <repository-url>
cd NexusTeam-Develop/Nexus-Team
```

---

## Docker Installation

### Step 1: Start Docker Infrastructure

Navigate to the project root and start all services:

```bash
cd Nexus-Team

# Start all services in the background
docker-compose up -d
```

**What this starts:**
- **Oracle XE 21c** - User database (port 1530)
- **MongoDB Sharded Cluster** - Message/chat database (port 27018)
- **Redis** - Cache and sessions (port 6380)
- **Database Seeder** - Automatically creates tables, collections, and indexes

### Step 2: Monitor Initialization

Watch the database seeder logs to ensure everything initializes correctly:

```bash
# Watch seeder logs
docker-compose logs -f db-seeder
```

**Wait for:**
```
Success: Database seeding completed successfully!
```

**Expected Services:**
```bash
# Check running services
docker-compose ps
```

You should see 7 services running:
- `nexusteam_oracle`
- `nexusteam_redis`
- `nexusteam_mongo_config`
- `nexusteam_mongo_shard1`
- `nexusteam_mongo_shard2`
- `nexusteam_mongos`
- `nexusteam_db_seeder` (completes and exits)

### Step 3: Verify Services

**Check MongoDB:**
```bash
mongosh mongodb://localhost:27018
# Should connect successfully
```

**Check Oracle:**
```bash
# Using SQL*Plus or Oracle SQL Developer
# Connection: localhost:1530/XEPDB1
# User: nexusteam_admin
# Password: 060707
```

**Check Redis:**
```bash
redis-cli -h localhost -p 6380 ping
# Expected: PONG
```

---

## Application Configuration

### Server Configuration

The `src/NexusTeam.Server/appsettings.json` is **already pre-configured** for Docker:

```json
{
  "Oracle": {
    "ConnectionString": "User Id=nexusteam_admin;Password=060707;Data Source=localhost:1530/XEPDB1",
    "CommandTimeout": 30,
    "MaxRetryAttempts": 3
  },
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27018",
    "DatabaseName": "NexusTeam",
    "ConnectionTimeout": 10,
    "ServerSelectionTimeout": 5
  },
  "Redis": {
    "ConnectionString": "localhost:6380",
    "DefaultDatabase": -1,
    "ConnectTimeout": 5000,
    "SyncTimeout": 5000,
    "AbortOnConnectFail": false
  }
}
```

**No configuration changes needed!** The application is ready to connect to Docker services.

> [!NOTE]
> For production, change the JWT secret key and database passwords in `appsettings.json` and `docker-compose.yaml`.

### Client Configuration

The client configuration (`src/NexusTeam.Client/appsettings.json`) is minimal and doesn't require changes.

---

## Building and Running the Application

### Step 1: Build the Solution

```powershell
# From the project root
cd Nexus-Team

# Restore dependencies
dotnet restore

# Build the solution
dotnet build Nexus-Team.sln

# Or build in Release mode
dotnet build Nexus-Team.sln -c Release
```

**Expected Output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Step 2: Run the Server

```powershell
# Navigate to the server directory
cd src\NexusTeam.Server

# Run the server (specify port as argument)
dotnet run 5251

# Or run the compiled executable
.\bin\Debug\net8.0\nexusteam_server.exe 5251
```

**Server Output:**
```
Server started on port 5251
Process ID: 12345
[Information] NexusTeam Server started successfully on port 5251, PID: 12345
```

**Verify Server:**
- Open browser: `http://localhost:5251`
- Should see: `{"Service":"NexusTeam Server","Status":"Running","Version":"1.0.0"}`
- Swagger UI: `http://localhost:5251/swagger`

### Step 3: Run the Client

```powershell
# In a new terminal, navigate to the client directory
cd src\NexusTeam.Client

# Build the client first
dotnet build

# Run the client (specify server IP and port)
dotnet run localhost 5251

# Or run compiled executable
.\bin\Debug\net8.0-windows\NexusTeam.exe localhost 5251
```

**Client Arguments:**
- First argument: Server IP address or hostname (e.g., `localhost`, `127.0.0.1`)
- Second argument: Server port number (e.g., `5251`, `5000`)

**Client Startup:**
1. Application window opens
2. If session exists, automatically logs in
3. Otherwise, shows welcome/login screen
4. Logs are written to: `%LocalAppData%\NexusTeam\Logs\`

---

## Verification

### 1. Check Docker Services

```bash
# Check all services are running
docker-compose ps

# Check service health
docker-compose ps --format "table {{.Name}}\t{{.Status}}\t{{.Health}}"
```

### 2. Test Database Connections

**MongoDB:**
```bash
mongosh mongodb://localhost:27018
use NexusTeam
show collections
# Should see: messages, chats, attachments, user_preferences, chat_folders, generated_images
```

**Oracle:**
```sql
-- Connect as nexusteam_admin
sqlplus nexusteam_admin/060707@localhost:1530/XEPDB1

-- Check users table exists
SELECT COUNT(*) FROM users;
```

**Redis:**
```bash
redis-cli -h localhost -p 6380
PING
# Should return: PONG
```

### 3. Test Application

1. **Start the server** (see above)
2. **Start the client** (see above)
3. **Register a new user** through the client
4. **Create a chat** and send messages
5. **Verify real-time messaging** works

---

## Troubleshooting

### Docker Issues

**Issue:** Docker services won't start

```bash
# Check Docker is running
docker ps

# Check Docker Compose version
docker-compose --version

# View detailed logs
docker-compose logs

# Restart services
docker-compose down
docker-compose up -d
```

**Issue:** Port conflicts

```bash
# Check which ports are in use
netstat -ano | findstr :1530
netstat -ano | findstr :27018
netstat -ano | findstr :6380

# Stop conflicting services or change ports in docker-compose.yaml
```

**Issue:** Database seeder fails

```bash
# Check seeder logs
docker-compose logs db-seeder

# Restart seeder
docker-compose restart db-seeder

# Or rebuild and restart
docker-compose up -d --build db-seeder
```

**Issue:** "Table or view does not exist" (Oracle)

```bash
# The seeder might have failed. Restart it:
docker-compose restart db-seeder

# Wait for completion
docker-compose logs -f db-seeder
```

**Issue:** MongoDB connection refused

```bash
# Check MongoDB services are healthy
docker-compose ps mongos mongo-shard1 mongo-shard2 mongo-config

# Check logs
docker-compose logs mongos

# Restart MongoDB services
docker-compose restart mongos mongo-shard1 mongo-shard2 mongo-config
```

**Issue:** Redis connection refused

```bash
# Check Redis is running
docker-compose ps redis

# Check logs
docker-compose logs redis

# Restart Redis
docker-compose restart redis
```

### Application Issues

**Issue:** Server cannot connect to databases

```powershell
# Verify Docker services are running
docker-compose ps

# Check connection strings in appsettings.json match Docker ports:
# - Oracle: localhost:1530
# - MongoDB: localhost:27018
# - Redis: localhost:6380

# Test connections manually
Test-NetConnection -ComputerName localhost -Port 1530
Test-NetConnection -ComputerName localhost -Port 27018
Test-NetConnection -ComputerName localhost -Port 6380
```

**Issue:** Client cannot connect to server

```powershell
# Verify server is running
# Check server logs for errors
# Verify firewall settings
# Ensure correct IP and port in client arguments

# Test server connectivity
Test-NetConnection -ComputerName localhost -Port 5251
```

**Issue:** Build errors

```powershell
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build

# Check for missing NuGet packages
dotnet restore --force

# Clear NuGet cache
dotnet nuget locals all --clear
```

**Issue:** .NET SDK not found

```powershell
# Verify .NET 8.0 SDK is installed
dotnet --version

# Check installed SDKs
dotnet --list-sdks

# Install .NET 8.0 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
```

### Common Docker Commands

```bash
# Start all services
docker-compose up -d

# Stop all services
docker-compose down

# Stop and remove volumes (⚠ Deletes Data)
docker-compose down -v

# View logs
docker-compose logs -f [service_name]

# Restart a service
docker-compose restart [service_name]

# Rebuild and restart
docker-compose up -d --build [service_name]

# Check service status
docker-compose ps

# Execute command in container
docker-compose exec [service_name] [command]
```

---

## Additional Information

### Docker Services Overview

| Service | Container Name | Port | Purpose |
|---------|----------------|------|---------|
| Oracle | `nexusteam_oracle` | 1530 | User database |
| MongoDB Router | `nexusteam_mongos` | 27018 | MongoDB access point |
| MongoDB Config | `nexusteam_mongo_config` | 27019 | Config server |
| MongoDB Shard 1 | `nexusteam_mongo_shard1` | 27020 | Shard 1 |
| MongoDB Shard 2 | `nexusteam_mongo_shard2` | 27021 | Shard 2 |
| Redis | `nexusteam_redis` | 6380 | Cache and sessions |
| DB Seeder | `nexusteam_db_seeder` | - | Database initialization |

### Database Credentials (Development)

**Oracle:**
- Host: `localhost:1530`
- Service: `XEPDB1`
- User: `nexusteam_admin`
- Password: `060707`

**MongoDB:**
- Connection: `mongodb://localhost:27018`
- Database: `NexusTeam`
- Authentication: None (development only)

**Redis:**
- Host: `localhost:6380`
- Authentication: None (development only)

> [!WARNING]
> **Production Security:** Change all default passwords and enable authentication for production deployments!

### Data Persistence

Docker volumes are used for data persistence:
- `nexusteam_oracle_data` - Oracle database files
- `nexusteam_redis_data` - Redis data
- `nexusteam_mongo_config_data` - MongoDB config server data
- `nexusteam_mongo_shard1_data` - MongoDB shard 1 data
- `nexusteam_mongo_shard2_data` - MongoDB shard 2 data

**Data Location:**
- Windows: `\\wsl$\docker-desktop-data\data\docker\volumes\`
- Linux/Mac: `/var/lib/docker/volumes/`

**Backup Data:**
```bash
# Backup volumes
docker run --rm -v nexusteam_oracle_data:/data -v $(pwd):/backup alpine tar czf /backup/oracle_backup.tar.gz /data
```

### Environment Variables

You can override Docker configuration using environment variables or `.env` file:

```bash
# Create .env file
ORACLE_PASSWORD=your_password
APP_USER_PASSWORD=your_password
```

### Next Steps

After successful installation:

1. **Create a user account** - Register through the client application
2. **Start creating chats** - Create direct messages or group chats
3. **Send messages** - Start messaging with real-time delivery
4. **Attach files** - Upload and share files
5. **Customize preferences** - Set themes, notifications, and privacy settings
6. **Explore API** - Use Swagger UI at `/swagger` endpoint

### Production Deployment Considerations

For production deployment:

1. **Change Default Passwords** - Update passwords in `docker-compose.yaml` and `appsettings.json`
2. **Enable Database Authentication** - Configure MongoDB and Redis authentication
3. **Use Environment Variables** - Store sensitive data in environment variables
4. **Enable HTTPS** - Configure SSL/TLS certificates
5. **Configure Firewall** - Restrict access to necessary ports only
6. **Set Up Monitoring** - Implement logging and monitoring solutions
7. **Backup Strategy** - Implement regular database backups
8. **Load Balancing** - Configure load balancer for multiple servers
9. **Resource Limits** - Set appropriate CPU and memory limits in Docker

### Support

If you encounter issues not covered in this guide:

1. Check application logs (server and client)
2. Check Docker service logs: `docker-compose logs`
3. Ensure all Docker services are running: `docker-compose ps`
4. Verify configuration correctness in `appsettings.json`
5. Review [Docker.md](Docker.md) for Docker-specific information
6. Review [ARCHITECTURE.md](ARCHITECTURE.md) for system overview
7. Review [SECURITY.md](SECURITY.md) for security considerations
8. Open an issue on GitHub with detailed information

---

**Installation Complete!**

You're now ready to use NexusTeam. Start the server and client, create an account, and begin chatting!
