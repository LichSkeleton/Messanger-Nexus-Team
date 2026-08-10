# Nexus Team - Real-Time Chat Application

<div align="center">

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-512BD4?logo=dotnet)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)
![Responsive](https://img.shields.io/badge/UI-Responsive%20Web-38d39f)
![License](https://img.shields.io/badge/license-MIT-blue.svg)

**A modern, scalable real-time chat application built with .NET 8 — WebSocket messaging, a multi-database backend, and a responsive web client that works on desktop and mobile. The entire stack runs in Docker with a single command.**

[Run](#run) • [Features](#features) • [Documentation](#documentation) • [Architecture](#architecture)

</div>

---

## Run

The **entire application — databases, the .NET Web API, and the responsive web client — runs in Docker with a single command.** You do not need to install .NET, run `dotnet run`, or use the Windows WPF client. Everything is containerized so it deploys the same way on any machine.

### Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| [Docker Desktop](https://www.docker.com/products/docker-desktop) | latest | Allocate **≥ 8 GB RAM** (Settings → Resources) |
| Git | any | To clone the repo |

> That's it — no local .NET SDK required. The API is built inside its container.

### 1. Clone and enter the project

```bash
git clone <repository-url>
cd Messanger/Nexus-Team
```

### 2. Launch everything

```bash
docker compose up -d --build
```

This one command builds and starts the whole stack:

- **Oracle** (users & auth), the **MongoDB** sharded cluster (chats/messages), and **Redis** (sessions/presence/cache)
- a one-time **seeder** that creates every table, collection, and index — plus **demo accounts with sample chat history** (see [Demo accounts](#demo-accounts))
- the **.NET 8 Web API** (`server`) exposing REST + WebSocket
- the **Nginx web client** (`web`) — the responsive universal UI

The first build takes a few minutes (it compiles the API and pulls the database images). Later runs start in seconds.

### 3. Open the app

Open **http://localhost:8080** in any browser — **on your desktop or on your phone.**

The web client is **fully responsive**: on a phone the chat list and the open conversation each take the full screen (like a Telegram-style mobile web app), while on a desktop they sit side by side. This single web UI is the **universal client** — there is nothing else to install.

Log in with one of the [demo accounts](#demo-accounts) below (or register your own) and start messaging in real time.

> **Using it from a phone on your network:** open `http://<your-computer-ip>:8080` (e.g. `http://192.168.1.20:8080`) from the phone's browser while it's on the same Wi-Fi.

### 4. (Optional) Check status and logs

```bash
docker compose ps                 # all services should be "healthy"/"running"
docker compose logs -f web server # follow frontend + API logs
```

`nexusteam_db_seeder` will show `Exited (0)` once seeding is done; `server` and `web` should report `healthy`.

### Stopping

```bash
docker compose down       # stop everything (keeps data)
docker compose down -v    # stop and wipe all database volumes (fresh start + re-seed)
```

---

## Demo accounts

Every time the databases are seeded (from scratch), the seeder creates the same four users with a short chat history between them, so you can log in and try the app immediately without registering.

| Username | Password |
|----------|----------|
| `Vlad`   | `Aa123456` |
| `Sofia`  | `Aa123456` |
| `Hakan`  | `Aa123456` |
| `Anna`   | `Aa123456` |

The seeded chats include four private conversations (Vlad↔Sofia, Hakan↔Anna, Sofia↔Anna, Vlad↔Hakan) and one group chat (`NexusTeam Devs`) containing all four users.

> These demo accounts are recreated on every seed run. They're for local development only — do not ship them to production.

---

## What's running where

Everything below runs as a Docker container started by `docker compose up -d --build`.

| Service | Host port | Purpose |
|---------|-----------|---------|
| **Web client (Nginx)** | **`8080`** | **Responsive universal UI — open this in any browser** |
| Server (.NET API) | `5251` | REST API + WebSocket (also reachable directly; Swagger via code) |
| Oracle 23ai Free | `1530` | Users & authentication (`FREEPDB1`) |
| MongoDB router (mongos) | `27018` | Messages, chats, attachments, preferences |
| Redis | `6380` | Sessions, presence, cache, rate limiting |

The browser only ever talks to the web client on port `8080`; Nginx reverse-proxies `/api` and `/ws` to the `server` container, so the app is same-origin and needs no extra configuration.

**Default dev credentials** (Oracle): user `nexusteam_admin`, password `060707`, service `FREEPDB1`. MongoDB and Redis have no auth in dev.

Every image is published for both `linux/amd64` and `linux/arm64`, so these exact commands work on Windows, Intel macOS, Apple Silicon and Linux. All host ports and passwords can be overridden by copying `Nexus-Team/.env.example` to `Nexus-Team/.env`.

> These are development defaults only. Change all passwords and enable authentication before any production use — see [SECURITY.md](Nexus-Team/docs/SECURITY.md).

---

## Troubleshooting

**`container nexusteam_oracle is unhealthy`** — almost always too little memory. Oracle needs ~2 GB, so give Docker Desktop at least 6 GB under Settings → Resources. Check the reason with `docker compose logs oracle`. If the volume was created by an older Oracle XE checkout, its datafiles cannot be opened by 23ai; reset cleanly:

```bash
docker compose down -v
docker volume rm nexusteam_oracle_data   # leftover from the old Oracle XE setup
docker compose up -d --build
```

**Port already in use** — free `8080`, `5251`, `1530`, `27018`, or `6380`, or copy `Nexus-Team/.env.example` to `Nexus-Team/.env` and change the port there.

**`http://localhost:8080` shows 502 Bad Gateway** — the API container is still starting. Wait for `docker compose ps` to show `server` as `healthy`, then refresh. On first launch the API build + Oracle startup can take a few minutes.

**Web client loads but login fails to connect** — check the API is up with `docker compose logs server` and confirm it printed `Server started on port 5251`.

**Seeder failed / "table or view does not exist"** — re-run it after the DBs are healthy:

```bash
docker compose build db-seeder
docker compose up db-seeder --force-recreate
docker compose logs db-seeder   # look for "Seeded user 'Vlad'" and demo accounts list
```

**Demo login fails / no users or chats** — the seeder image may be stale. Rebuild and re-run as above, then look for `Step 4/4: Seeding demo data` in the logs.

**Server can't reach a database** — confirm `docker compose ps` is all healthy, then test connectivity (PowerShell): `Test-NetConnection localhost -Port 1530`.

More fixes: [INSTALLATION.md → Troubleshooting](Nexus-Team/docs/INSTALLATION.md#troubleshooting).

---

## Features

### Core functionality

- **User Authentication** — secure registration/login with JWT and refresh tokens
- **Real-Time Messaging** — WebSocket delivery with automatic reconnection
- **Voice Messages** — record and send voice notes from the web client (`MediaRecorder`)
- **Voice Calls** — WebRTC-based calling in the optional WPF Windows client (NAudio)
- **Chat Types** — direct messages, group chats, and channels
- **Message Management** — send, edit, delete, reply/threading, emoji reactions
- **Presence & Receipts** — online/away/DND status, sent/delivered/read receipts
- **File Attachments** — upload and share multiple file types
- **Image Generation** — AI-powered image generation
- **Organization** — chat folders, code preview with syntax highlighting, message translation
- **Preferences** — customizable themes, notifications, and privacy settings

### Technical highlights

- **Multi-database architecture** — Oracle (users), MongoDB (messages/chats/attachments/preferences), Redis (cache/sessions/presence)
- **Scalable & stateless** — JWT-based API servers support horizontal scaling
- **Security first** — BCrypt hashing, rate limiting, security headers, FluentValidation
- **Performance** — indexed queries, connection pooling, intelligent caching, cursor pagination
- **API docs** — Swagger/OpenAPI with JWT Bearer auth

---

## Solution structure

```
Messanger/
├── README.md                  # You are here
├── LICENSE
└── Nexus-Team/
    ├── docker-compose.yaml    # Full stack: DBs, seeder, API server, web client
    ├── Nexus-Team.sln
    ├── web/                    # Responsive web client (HTML/CSS/JS) + Nginx
    │   ├── index.html
    │   ├── styles.css
    │   ├── app.js
    │   ├── nginx.conf          # Serves the SPA + proxies /api and /ws
    │   └── Dockerfile
    ├── src/
    │   ├── NexusTeam.Shared/   # Shared models, DTOs, contracts, helpers
    │   ├── NexusTeam.Server/   # ASP.NET Core API + WebSocket (+ Dockerfile)
    │   ├── NexusTeam.Client/   # WPF desktop app (MVVM) — optional, Windows-only
    │   └── NexusTeam.DbSeeder/ # One-time DB initialization
    ├── config/                # DB scripts & collection configs (oracle, mongodb, redis)
    ├── scripts/powershell/    # Provisioning scripts (manual setup)
    └── docs/                   # Detailed documentation
```

---

## Technology stack

| Component | Technology | Version |
|-----------|------------|---------|
| Runtime | .NET | 8.0 |
| Server | ASP.NET Core | 8.0 |
| Primary client | Responsive web SPA (HTML/CSS/JS) + Nginx | any modern browser |
| Optional client | WPF desktop (Windows-only) | 8.0-windows |
| User DB | Oracle Database | 23ai Free |
| Message DB | MongoDB (sharded) | 7.x |
| Cache/Sessions | Redis | 7.x |
| Auth | JWT | - |
| Password Hashing | BCrypt | - |
| Logging | Serilog | latest |
| Validation | FluentValidation | latest |
| API Docs | Swagger/OpenAPI | latest |
| MVVM Framework (WPF) | CommunityToolkit.Mvvm | latest |
| Voice calls (WPF) | NAudio (audio) + WebSocket (signaling) | latest |
| Voice messages (web) | MediaRecorder API | browser-native |

---

## Architecture

```
┌──────────────────────────┐      ┌──────────────────────────┐
│  Web client (Nginx)      │      │  WPF Client (MVVM)        │
│  Responsive SPA — any     │      │  Optional, Windows-only   │
│  desktop/mobile browser   │      │  Voice Calls (WebRTC)     │
│  :8080  → proxies /api,/ws│      │                           │
└────────────┬──────────────┘      └────────────┬─────────────┘
             │        HTTP/REST + WebSocket      │
┌────────────┴───────────────────────────────────┴────────────┐
│              ASP.NET Core Web API Server  (:5251)            │
│           REST Controllers + WebSocket Handler               │
└───────┬───────────────┬───────────────┬─────────────────────┘
        │               │               │
    ┌───▼───┐       ┌───▼───┐       ┌───▼────┐
    │Oracle │       │MongoDB│       │ Redis  │
    │Users  │       │Chats/ │       │Cache/  │
    │       │       │Msgs   │       │Sessions│
    └───────┘       └───────┘       └────────┘
```

See [ARCHITECTURE.md](Nexus-Team/docs/ARCHITECTURE.md) for the full design.

---

## Documentation

| Doc | What's inside |
|-----|---------------|
| [Installation](Nexus-Team/docs/INSTALLATION.md) | Full setup, verification, and troubleshooting |
| [Architecture](Nexus-Team/docs/ARCHITECTURE.md) | System design, data model, communication protocols |
| [Server](Nexus-Team/docs/SERVER.md) | Server internals and configuration |
| [Client](Nexus-Team/docs/CLIENT.md) | Client internals and configuration |
| [Docker](Nexus-Team/docs/Docker.md) | Docker infrastructure details |
| [Security](Nexus-Team/docs/SECURITY.md) | Security model and production hardening |

---

## Security

Enterprise-grade security throughout: JWT auth with refresh tokens, BCrypt password hashing, rate limiting against brute-force/spam, FluentValidation on all endpoints, security headers (XSS/clickjacking/MIME-sniffing protection), configurable CORS, and audit logging of message edits/deletes. Details in [SECURITY.md](Nexus-Team/docs/SECURITY.md).

---

## Authors

**Nexus Team**

- **Vladyslav Zaplitnyi**
- **Anna Kornet**
- **Sofiia Khyzhnychenko**
- **Halil Hakan Karabay**

---

## License

Licensed under the MIT License — see [LICENSE](LICENSE).

---

<div align="center">

**Made with care by the Nexus Team**

</div>
