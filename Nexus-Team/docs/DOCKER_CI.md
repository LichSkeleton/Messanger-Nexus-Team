# Docker CI platform coverage

The repository uses two GitHub Actions workflows to verify the Docker stack.

## Pull request and push checks

`.github/workflows/docker-platform-ci.yml` runs automatically when Docker,
server, seeder, web, or configuration files change. It:

1. validates `docker-compose.yaml`;
2. builds the server, DB seeder, and web images for `linux/amd64` and
   `linux/arm64`;
3. starts and verifies the complete stack on native Linux x64 and Linux ARM64
   GitHub-hosted runners;
4. checks the seeder twice and performs a warm restart against populated
   volumes.

Windows and macOS use these Linux images through Docker Desktop's Linux VM.
The two image architectures therefore cover Windows x64, Windows ARM64,
Intel macOS, Apple Silicon, Linux x64, and Linux ARM64 hosts.

## Real Docker Desktop host checks

`.github/workflows/docker-desktop-hosts.yml` is a nightly and manually
dispatchable test for real Windows and macOS machines. It is disabled until
self-hosted runners have been registered, so missing runners cannot leave jobs
queued indefinitely.

Register runners with these labels in addition to GitHub's default labels:

| Host | Required runner labels |
| --- | --- |
| Windows x64 | `self-hosted`, `Windows`, `X64`, `docker-desktop` |
| Windows ARM64 | `self-hosted`, `Windows`, `ARM64`, `docker-desktop` |
| Intel macOS | `self-hosted`, `macOS`, `X64`, `docker-desktop` |
| Apple Silicon | `self-hosted`, `macOS`, `ARM64`, `docker-desktop` |

Each runner must have:

- Docker Desktop running in Linux container mode;
- Docker Compose v2 available as `docker compose`;
- Bash, curl, and Git available to the runner account;
- enough free disk and memory for Oracle, MongoDB, Redis, the API, and web
  containers.

After all desired runners are online, create the repository Actions variable
`ENABLE_DOCKER_DESKTOP_RUNNERS` with the value `true`. The workflow can then be
started from **Actions > Docker Desktop Host Matrix > Run workflow**. If only a
subset of the four hosts is available, remove the unavailable matrix entries
before enabling the variable.

The host workflow removes only this Compose project's containers and named
volumes before and after a run. Use dedicated CI machines rather than developer
workstations because Docker Desktop and the self-hosted runner must remain
available unattended.
