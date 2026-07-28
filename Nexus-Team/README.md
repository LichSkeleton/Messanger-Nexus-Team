# NexusTeam

The full project documentation — quick start, demo accounts, architecture, and troubleshooting — lives in the **[root README](../README.md)**.

The **whole application (databases + .NET Web API + responsive web client) runs in Docker** — no local .NET SDK and no `dotnet run` needed. From this folder (`Nexus-Team/`):

```bash
docker compose up -d --build
```

Then open **http://localhost:8080** in any browser (desktop or mobile). The web UI is fully responsive and is the universal client — nothing else to install.

Stop with `docker compose down` (add `-v` to also wipe the databases).

**Demo logins** (password for all: `Aa123456`): `Pavalo`, `Olen`, `Vlad` — see [Demo accounts](../README.md#demo-accounts) in the root README.

> The WPF desktop client (`src/NexusTeam.Client`, Windows-only) is still available for development, but it is no longer required to use the app.
