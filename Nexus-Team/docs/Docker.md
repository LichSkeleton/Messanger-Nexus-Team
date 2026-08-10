# NexusTeam Docker Infrastructure

The whole application — web client, API, and all three databases — runs from a
single Docker Compose file. Every image is published for both `linux/amd64` and
`linux/arm64`, so the same commands work on Windows, Intel macOS, Apple Silicon
and Linux with no per-machine edits.

---

## Quick Start

### 1. Prerequisites

- **Docker Desktop** (or Docker Engine + Compose v2) with **at least 6 GB of RAM**
  allocated. Oracle alone wants ~2 GB.
- About **6 GB of free disk** for images and volumes.

### 2. Start

```bash
cd Nexus-Team
docker compose up -d --build
```

Then open **<http://localhost:8080>** in any browser, on the host or on a phone
pointed at the host's LAN address.

The first start pulls ~2 GB of images and takes roughly 2–4 minutes. Later
starts take about 30 seconds.

### 3. Sign in

The seeder creates four demo accounts, all sharing the password `Aa123456`:

`Vlad`, `Sofia`, `Hakan`, `Anna`

### 4. Stop

```bash
docker compose down          # keeps your data
docker compose down -v       # also deletes all database volumes
```

---

## What Runs

| Service | Purpose | Host port |
| :--- | :--- | :--- |
| `web` | Nginx serving the SPA, reverse-proxying `/api` and `/ws` | **8080** |
| `server` | .NET 8 REST + WebSocket API | 5251 |
| `oracle` | Oracle Database 23ai Free — users | 1530 |
| `mongos` | MongoDB sharded cluster router — chats, messages | 27018 |
| `redis` | Cache, sessions, presence | 6380 |
| `mongo-config`, `mongo-shard1`, `mongo-shard2` | Cluster members | internal only |
| `mongo-init`, `mongo-router-init`, `db-seeder` | One-shot bootstrap jobs | — |

Only `web` needs to be reachable to use the app. The database ports are
published for convenience when running the .NET server or the WPF client
outside Docker.

Startup order is enforced with health checks, so `docker compose up` does not
return until the API is actually answering requests:

```
mongo-config/shard1/shard2 -> mongo-init -> mongos -> mongo-router-init ─┐
oracle ─────────────────────────────────────────────────────────────────┼─> db-seeder -> server -> web
redis  ─────────────────────────────────────────────────────────────────┘
```

---

## Configuration

Everything works with no configuration. To change a port or a password, copy
`.env.example` to `.env` and edit it:

```bash
cp .env.example .env          # macOS / Linux
Copy-Item .env.example .env   # Windows PowerShell
```

| Variable | Default | Notes |
| :--- | :--- | :--- |
| `WEB_PORT` | `8080` | The address you open in the browser |
| `SERVER_PORT` | `5251` | API, also used by the WPF client |
| `ORACLE_PORT` / `REDIS_PORT` / `MONGO_PORT` | `1530` / `6380` / `27018` | Change these if a port is taken |
| `ORACLE_SERVICE` | `FREEPDB1` | Oracle 23ai Free's pluggable database name |
| `ORACLE_APP_USER` / `ORACLE_APP_PASSWORD` | `nexusteam_admin` / `060707` | Also used by `appsettings.json` for out-of-container runs |
| `JWT_SECRET` | dev value | Replace before exposing the stack beyond localhost |

---

## Apple Silicon and other architectures

The database is **Oracle Database 23ai Free** (`gvenzl/oracle-free:23-slim-faststart`).
Two reasons, both about portability and stability:

- The older `gvenzl/oracle-xe` images are **built for amd64 only**. On an M-series
  Mac they run under QEMU emulation, which is slow enough that health checks time
  out and the stack fails to come up. `oracle-free` ships a native arm64 image.
- The `-faststart` variant contains a pre-created database, so first boot is
  ~30 seconds instead of the several minutes Oracle normally spends running DBCA.

Oracle 23ai names its pluggable database **`FREEPDB1`**, where XE used `XEPDB1`.
All connection strings in `docker-compose.yaml` and `appsettings.json` use the
new name.

> **Upgrading from an older checkout:** Oracle 23ai cannot open datafiles created
> by XE 21. The Oracle volume was therefore renamed to `nexusteam_oracle_free_data`,
> so a stale XE volume can no longer break your start. Reclaim the old one with
> `docker volume rm nexusteam_oracle_data`.

---

## Common Commands

```bash
docker compose ps                     # status + health of every service
docker compose logs -f server         # follow API logs
docker compose logs db-seeder         # confirm the databases were seeded
docker compose restart server         # restart one service
docker compose up -d --build server   # rebuild after changing C# code
docker compose down -v && docker compose up -d --build   # full reset
```

---

## Troubleshooting

| Symptom | Cause and fix |
| :--- | :--- |
| `port is already allocated` | Something else uses 8080/5251/1530/6380/27018. Copy `.env.example` to `.env` and change the port. |
| Oracle stuck at `starting` / health check never passes | Give Docker Desktop more memory (Settings → Resources → at least 6 GB). Follow with `docker compose logs oracle`. |
| Oracle fails with `ORA-00845` | `/dev/shm` too small. The compose file sets `shm_size: 1gb`; make sure you are not overriding it. |
| `db-seeder` exits non-zero | Read `docker compose logs db-seeder`. It retries for 150 s, so a real failure is a schema or credentials problem, not a timing one. |
| Web page loads but every request 502s | The API is not up yet. `docker compose ps` should show `nexusteam_server` as `healthy`. |
| "Table or view does not exist" | Seeder never ran against this volume: `docker compose up -d --force-recreate db-seeder`. |
| Everything is broken after switching branches | `docker compose down -v && docker compose up -d --build`. |
