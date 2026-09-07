# VPN deployment

`deploy.sh` deploys a committed `Nexus-Team` revision to a Docker host reached
through SSH over an already-connected VPN. It uploads a release archive, builds
the new images, gracefully stops the previous Compose stack without deleting
named volumes, starts the new release, waits for the API and web health checks,
obtains or renews a trusted Let's Encrypt certificate for the public IP address,
and rolls back to the previous release if startup fails.

## Server prerequisites

- Linux server reachable through the VPN by IPv4 address or DNS name
- SSH public-key authentication
- Docker Engine and Docker Compose v2
- At least 8 GB RAM for Oracle and the rest of the stack
- An SSH user that can run Docker without `sudo`
- A deployment directory owned by that SSH user

One-time server preparation, after connecting the VPN:

```bash
ssh deploy@10.20.0.15
sudo mkdir -p /opt/nexus-team
sudo chown deploy:deploy /opt/nexus-team
docker version
docker compose version
exit
```

The first SSH connection must be made manually so the server host key can be
verified and added to `known_hosts`. The deployment script deliberately uses
strict host-key checking.

## Production secrets

Create the ignored production environment file locally:

```bash
cd Nexus-Team
cp .env.production.example .env.production
```

Generate independent secrets and paste them into `.env.production`:

```bash
openssl rand -base64 48
openssl rand -base64 48
openssl rand -base64 48
```

Also replace both Oracle passwords. The script refuses to deploy development or
`CHANGE_ME` values. On first deployment the file is uploaded to
`/opt/nexus-team/shared/.env` with mode `0600`. Later deployments reuse that
server file when `DEPLOY_ENV_FILE` is set to an empty value.

Set `PUBLIC_IP` to the server's stable public IPv4 address. `LETSENCRYPT_EMAIL`
is optional but recommended for certificate-account notices. Production binds
the application and data-service ports to `127.0.0.1`; only the TLS gateway and
TURN relay ports are public.

## Deploy

Connect the VPN, then run from `Nexus-Team`:

```bash
DEPLOY_HOST=10.20.0.15 \
DEPLOY_USER=deploy \
DEPLOY_SSH_KEY="$HOME/.ssh/nexus_deploy" \
./deploy.sh
```

Defaults:

| Variable | Default | Purpose |
|---|---|---|
| `DEPLOY_PORT` | `22` | SSH port |
| `DEPLOY_PATH` | `/opt/nexus-team` | Persistent deployment root |
| `DEPLOY_REF` | `origin/main` | Committed Git revision to archive |
| `DEPLOY_ENV_FILE` | `.env.production` | Local production secrets file |
| `DEPLOY_KEEP_RELEASES` | `3` | Number of release directories retained |
| `DEPLOY_HEALTH_TIMEOUT` | `600` | Health-check timeout in seconds |
| `DEPLOY_FETCH` | `1` | Fetch origin before resolving the revision |
| `DEPLOY_SSH_KEY` | SSH agent/default | Optional SSH private-key path |

To redeploy using the existing server-side secrets, set
`DEPLOY_ENV_FILE=` in the deployment command. To deploy an explicitly
checked-out commit without fetching:

```bash
DEPLOY_HOST=10.20.0.15 \
DEPLOY_USER=deploy \
DEPLOY_REF=HEAD \
DEPLOY_FETCH=0 \
./deploy.sh
```

Only committed files are deployed. Uncommitted tracked changes cause the script
to stop; untracked files and local secrets are never included in the archive.

## What happens during each deployment

1. Local tools, Git state, VPN/SSH connectivity, Docker access, and secrets are
   validated.
2. The selected commit's `Nexus-Team` tree is archived and uploaded to a new
   timestamped release directory.
3. New Docker images are built while the old containers continue serving.
4. The old stack receives `docker compose down --remove-orphans --timeout 120`.
   The command intentionally does not use `--volumes`, so Oracle, MongoDB, and
   Redis data remain intact.
5. The new stack starts and the script waits for `nexusteam_server` and
   `nexusteam_web` to become healthy.
6. The production gateway answers the HTTP-01 challenge, Certbot obtains a
   trusted six-day IP certificate, and Nginx activates HTTPS. A renewal sidecar
   checks every six hours and signals Nginx after renewal.
7. On success, `current` points to the new release and old release directories
   beyond the retention count are removed.
8. On failure, logs are printed, the failed stack is stopped, and the previous
   release is rebuilt and restarted.

## Operations

Inspect the active deployment:

```bash
ssh deploy@10.20.0.15
cd /opt/nexus-team/current
docker compose ps
docker compose logs --follow web server
```

The public application URL is `https://PUBLIC_IP`; port `80` only redirects to
HTTPS and serves ACME challenges. Keep API and database ports (`5251`, `1530`,
`27018`, and `6380`) bound to localhost. WebRTC calling additionally requires
public TCP/UDP `3478`, TCP `5349`, and UDP `49160-49200` to reach coturn.

The current `db-seeder` also recreates four demo accounts on every startup with
the repository-documented shared password. This is suitable for a VPN-only demo,
but the seeder must be changed or disabled before exposing the application to
untrusted users or the public internet.
