# 🐳 NexusTeam Docker Infrastructure

 **Professional Docker Setup for NexusTeam MVP**
 This guide covers the complete local development environment setup using Docker Compose.

---

##  Quick Start

Follow these steps to get the entire application stack running in minutes.

### 1. Prerequisites
Ensure you have the following installed:
- **Docker Desktop** (running with 8GB+ RAM allocated)
- **Git**

### 2. Start the Environment
Run these commands from the project root:

```powershell
# 1. Pull latest images and build the seeder
docker-compose build

# 2. Start services in the background
docker-compose up -d
```

### 3. Verification
Check that all services are healthy:

```powershell
docker-compose ps
```
 **Expected Output:**  
You should see 7 services running: `nexusteam_oracle`, `nexusteam_redis`, `nexusteam_mongos`, ...

 **Check Initialization:**
View logs for the database seeder to confirm everything is ready:
```powershell
docker-compose logs -f db-seeder
```
*Wait for: `Success: Database seeding completed successfully!`*

---

##  Typical Usage

### Connecting Your App
The Docker environment exposes these ports for your local application (Client/Server):

| Service | Host Address | Credentials |
| :--- | :--- | :--- |
| **MongoDB** | `localhost:27018` | No Auth (Dev) |
| **Oracle** | `localhost:1530` | `nexusteam_admin` / `060707` |
| **Redis** | `localhost:6380` | No Auth |

### Common Commands

**Restart a specific service:**
```powershell
docker-compose restart [service_name]
# Example: docker-compose restart nexusteam_server
```

**Stop everything:**
```powershell
docker-compose down
```

**Reset everything (⚠ Deletes Data):**
```powershell
docker-compose down -v
```

---

##  Architecture Overview

The infrastructure simulates a production-grade distributed system:

- **Entry Point:** `mongos` (Router) for seamless sharded data access.
- **Data Layer:** 
  - **Oracle 21c**: Identity & Relational Data.
  - **MongoDB Sharded Cluster**: Scalable Chat Data.
  - **Redis 7**: High-performance Caching & Pub/Sub.
- **Automation:** 
  - `mongo-init`: Bootstraps the cluster.
  - `mongo-router-init`: Connects shards.
  - `db-seeder`: Creates tables and indexes automatically.

---

##  Troubleshooting

| Issue | Solution |
| :--- | :--- |
| **"Table or view does not exist"** | The seeder might have failed. Run `docker-compose restart db-seeder`. |
| **Port Conflicts** | We map ports to `1530`, `6380`, `27018` to avoid conflicts. Ensure your app uses these ports. |
| **Seeder "Redis Admin" Error** | Already fixed in config (requires `allowAdmin=true`). No action needed. |
