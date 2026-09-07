#!/usr/bin/env bash

set -Eeuo pipefail
IFS=$'\n\t'

readonly SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
readonly REPOSITORY_ROOT="$(git -C "$SCRIPT_DIR" rev-parse --show-toplevel)"

log() {
  printf '[deploy] %s\n' "$*"
}

fail() {
  printf '[deploy] ERROR: %s\n' "$*" >&2
  exit 1
}

require_command() {
  command -v "$1" >/dev/null 2>&1 || fail "Required command not found: $1"
}

env_value() {
  local file="$1"
  local key="$2"

  awk -v expected_key="$key" '
    index($0, expected_key "=") == 1 {
      sub(/^[^=]*=/, "")
      print
      exit
    }
  ' "$file"
}

validate_environment_file() {
  local file="$1"
  local key value
  local -a required_keys=(
    PUBLIC_IP
    JWT_SECRET
    DEVICE_LOCK_PIN_PEPPER
    TURN_SECRET
    ORACLE_APP_PASSWORD
    ORACLE_SYS_PASSWORD
  )

  [[ -f "$file" ]] || fail "Environment file not found: $file"

  for key in "${required_keys[@]}"; do
    value="$(env_value "$file" "$key")"
    [[ -n "$value" ]] || fail "$key is missing or empty in $file"
    case "$value" in
      CHANGE_ME*|060707|a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6|local-device-lock-pepper-change-me-*|nexusteam_turn_secret)
        fail "$key still contains a development/default value in $file"
        ;;
    esac
  done

  [[ "$(env_value "$file" JWT_SECRET | wc -c | tr -d ' ')" -gt 32 ]] ||
    fail "JWT_SECRET must contain at least 32 characters"
  [[ "$(env_value "$file" DEVICE_LOCK_PIN_PEPPER | wc -c | tr -d ' ')" -gt 32 ]] ||
    fail "DEVICE_LOCK_PIN_PEPPER must contain at least 32 characters"
  [[ "$(env_value "$file" TURN_SECRET | wc -c | tr -d ' ')" -gt 32 ]] ||
    fail "TURN_SECRET must contain at least 32 characters"

  value="$(env_value "$file" PUBLIC_IP)"
  [[ "$value" =~ ^([0-9]{1,3}\.){3}[0-9]{1,3}$ ]] ||
    fail "PUBLIC_IP must be an IPv4 address"
}

for command_name in git ssh scp tar gzip awk; do
  require_command "$command_name"
done

: "${DEPLOY_HOST:?Set DEPLOY_HOST to the server VPN IP or VPN DNS name}"
: "${DEPLOY_USER:?Set DEPLOY_USER to the SSH user on the server}"

DEPLOY_PORT="${DEPLOY_PORT:-22}"
DEPLOY_PATH="${DEPLOY_PATH:-/opt/nexus-team}"
DEPLOY_REF="${DEPLOY_REF:-origin/main}"
DEPLOY_ENV_FILE="${DEPLOY_ENV_FILE-$SCRIPT_DIR/.env.production}"
DEPLOY_KEEP_RELEASES="${DEPLOY_KEEP_RELEASES:-3}"
DEPLOY_HEALTH_TIMEOUT="${DEPLOY_HEALTH_TIMEOUT:-600}"
DEPLOY_FETCH="${DEPLOY_FETCH:-1}"
DEPLOY_SSH_KEY="${DEPLOY_SSH_KEY:-}"

[[ "$DEPLOY_PORT" =~ ^[0-9]+$ ]] || fail "DEPLOY_PORT must be numeric"
[[ "$DEPLOY_KEEP_RELEASES" =~ ^[1-9][0-9]*$ ]] || fail "DEPLOY_KEEP_RELEASES must be a positive integer"
[[ "$DEPLOY_HEALTH_TIMEOUT" =~ ^[1-9][0-9]*$ ]] || fail "DEPLOY_HEALTH_TIMEOUT must be a positive integer"
[[ "$DEPLOY_USER" =~ ^[A-Za-z0-9._-]+$ ]] || fail "DEPLOY_USER contains unsupported characters"
[[ "$DEPLOY_HOST" =~ ^[A-Za-z0-9._-]+$ ]] || fail "DEPLOY_HOST must be a VPN IPv4 address or DNS name"
[[ "$DEPLOY_PATH" =~ ^/[A-Za-z0-9._/-]+$ ]] || fail "DEPLOY_PATH must be a simple absolute path without spaces"

case "$DEPLOY_PATH" in
  /|/bin|/boot|/dev|/etc|/home|/opt|/root|/srv|/usr|/var)
    fail "DEPLOY_PATH is too broad: $DEPLOY_PATH"
    ;;
esac

if [[ -n "$DEPLOY_SSH_KEY" ]]; then
  [[ -f "$DEPLOY_SSH_KEY" ]] || fail "SSH identity file not found: $DEPLOY_SSH_KEY"
fi

if [[ -n "$(git -C "$REPOSITORY_ROOT" status --porcelain --untracked-files=no -- Nexus-Team)" ]]; then
  fail "Tracked files under Nexus-Team have uncommitted changes; commit or stash them before deploying"
fi

if [[ "$DEPLOY_FETCH" == "1" ]]; then
  log "Fetching the latest origin refs..."
  git -C "$REPOSITORY_ROOT" fetch --prune origin
elif [[ "$DEPLOY_FETCH" != "0" ]]; then
  fail "DEPLOY_FETCH must be 0 or 1"
fi

commit_sha="$(git -C "$REPOSITORY_ROOT" rev-parse --verify "${DEPLOY_REF}^{commit}")" ||
  fail "Cannot resolve DEPLOY_REF: $DEPLOY_REF"
git -C "$REPOSITORY_ROOT" cat-file -e "${commit_sha}:Nexus-Team/docker-compose.yaml" ||
  fail "$DEPLOY_REF does not contain Nexus-Team/docker-compose.yaml"

readonly COMMIT_SHA="$commit_sha"
readonly RELEASE_ID="$(date -u +%Y%m%d%H%M%S)-${COMMIT_SHA:0:12}-$$"
readonly SSH_TARGET="${DEPLOY_USER}@${DEPLOY_HOST}"
readonly REMOTE_ARCHIVE="$DEPLOY_PATH/incoming/$RELEASE_ID.tar.gz"
readonly REMOTE_ENV="$DEPLOY_PATH/incoming/$RELEASE_ID.env"
readonly TEMPORARY_DIRECTORY="$(mktemp -d "${TMPDIR:-/tmp}/nexus-deploy.XXXXXX")"
readonly LOCAL_ARCHIVE="$TEMPORARY_DIRECTORY/$RELEASE_ID.tar.gz"

cleanup() {
  rm -rf -- "$TEMPORARY_DIRECTORY"
}
trap cleanup EXIT

ssh_options=(-p "$DEPLOY_PORT" -o BatchMode=yes -o ConnectTimeout=15 -o StrictHostKeyChecking=yes)
scp_options=(-P "$DEPLOY_PORT" -o BatchMode=yes -o ConnectTimeout=15 -o StrictHostKeyChecking=yes)
if [[ -n "$DEPLOY_SSH_KEY" ]]; then
  ssh_options+=(-i "$DEPLOY_SSH_KEY" -o IdentitiesOnly=yes)
  scp_options+=(-i "$DEPLOY_SSH_KEY" -o IdentitiesOnly=yes)
fi

log "Checking VPN/SSH connectivity to $SSH_TARGET..."
ssh "${ssh_options[@]}" "$SSH_TARGET" bash -s <<'REMOTE_CHECK'
set -Eeuo pipefail
command -v docker >/dev/null 2>&1 || { echo "Docker is not installed" >&2; exit 1; }
docker compose version >/dev/null 2>&1 || { echo "Docker Compose v2 is not available" >&2; exit 1; }
docker info >/dev/null 2>&1 || { echo "The SSH user cannot access the Docker daemon" >&2; exit 1; }
REMOTE_CHECK

log "Preparing remote release directories..."
ssh "${ssh_options[@]}" "$SSH_TARGET" bash -s -- "$DEPLOY_PATH" <<'REMOTE_PREPARE'
set -Eeuo pipefail
deploy_path="$1"
[[ "$deploy_path" =~ ^/[A-Za-z0-9._/-]+$ ]] || exit 1
case "$deploy_path" in
  /|/bin|/boot|/dev|/etc|/home|/opt|/root|/srv|/usr|/var) exit 1 ;;
esac
mkdir -p -- "$deploy_path/incoming" "$deploy_path/releases" "$deploy_path/shared"
REMOTE_PREPARE

env_uploaded=0
if [[ -f "$DEPLOY_ENV_FILE" ]]; then
  validate_environment_file "$DEPLOY_ENV_FILE"
  log "Uploading production environment file..."
  scp "${scp_options[@]}" "$DEPLOY_ENV_FILE" "$SSH_TARGET:$REMOTE_ENV"
  env_uploaded=1
else
  log "No local environment file supplied; checking the existing server environment..."
  ssh "${ssh_options[@]}" "$SSH_TARGET" test -s "$DEPLOY_PATH/shared/.env" ||
    fail "First deployment requires a readable DEPLOY_ENV_FILE"
fi

application_host="${PUBLIC_IP:-}"
if [[ -z "$application_host" && -f "$DEPLOY_ENV_FILE" ]]; then
  application_host="$(env_value "$DEPLOY_ENV_FILE" PUBLIC_IP)"
fi
application_host="${application_host:-$DEPLOY_HOST}"

log "Creating release archive for ${COMMIT_SHA:0:12}..."
git -C "$REPOSITORY_ROOT" archive --format=tar "${COMMIT_SHA}:Nexus-Team" | gzip -9 >"$LOCAL_ARCHIVE"

log "Uploading release $RELEASE_ID..."
scp "${scp_options[@]}" "$LOCAL_ARCHIVE" "$SSH_TARGET:$REMOTE_ARCHIVE"

log "Deploying release on the server..."
ssh "${ssh_options[@]}" "$SSH_TARGET" bash -s -- \
  "$DEPLOY_PATH" "$RELEASE_ID" "$DEPLOY_KEEP_RELEASES" "$DEPLOY_HEALTH_TIMEOUT" "$env_uploaded" <<'REMOTE_DEPLOY'
set -Eeuo pipefail

deploy_path="$1"
release_id="$2"
keep_releases="$3"
health_timeout="$4"
env_uploaded="$5"

release_dir="$deploy_path/releases/$release_id"
incoming_archive="$deploy_path/incoming/$release_id.tar.gz"
incoming_env="$deploy_path/incoming/$release_id.env"
shared_env="$deploy_path/shared/.env"
previous_env_backup="$deploy_path/incoming/$release_id.previous.env"
previous_release=""
stack_transition_started=0

log() {
  printf '[remote-deploy] %s\n' "$*"
}

if [[ -L "$deploy_path/current" ]]; then
  previous_release="$(readlink -f -- "$deploy_path/current" || true)"
fi

rollback() {
  log "New release failed; collecting logs and attempting rollback..."
  if [[ -s "$previous_env_backup" ]]; then
    install -m 600 -- "$previous_env_backup" "$shared_env"
  fi

  if [[ "$stack_transition_started" == "1" && -f "$release_dir/docker-compose.yaml" ]]; then
    (cd "$release_dir" && compose logs --no-color --tail=200) || true
    (cd "$release_dir" && compose down --remove-orphans --timeout 120) || true
  fi

  if [[ "$stack_transition_started" == "1" && -n "$previous_release" && -f "$previous_release/docker-compose.yaml" ]]; then
    log "Restarting previous release: $previous_release"
    ln -sfn -- "$previous_release" "$deploy_path/current"
    (cd "$previous_release" && compose up --detach --build --remove-orphans) || true
  elif [[ "$stack_transition_started" == "0" ]]; then
    log "The previous release was not stopped and remains active"
  fi

  exit 1
}

compose() {
  docker compose --profile production "$@"
}

wait_for_healthy_container() {
  local container_name="$1"
  local deadline=$((SECONDS + health_timeout))
  local status

  while ((SECONDS < deadline)); do
    status="$(docker inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}' "$container_name" 2>/dev/null || true)"
    case "$status" in
      healthy)
        log "$container_name is healthy"
        return 0
        ;;
      unhealthy|exited|dead)
        log "$container_name entered terminal state: $status"
        return 1
        ;;
    esac
    sleep 5
  done

  log "Timed out waiting for $container_name"
  return 1
}

[[ -f "$incoming_archive" ]] || { log "Release archive is missing"; exit 1; }

if [[ "$env_uploaded" == "1" ]]; then
  if [[ -s "$shared_env" ]]; then
    cp -- "$shared_env" "$previous_env_backup"
  fi
  install -m 600 -- "$incoming_env" "$shared_env"
fi
[[ -s "$shared_env" ]] || { log "Production environment file is missing"; exit 1; }

env_value() {
  local key="$1"

  awk -v expected_key="$key" '
    index($0, expected_key "=") == 1 {
      sub(/^[^=]*=/, "")
      print
      exit
    }
  ' "$shared_env"
}

validate_environment_file() {
  local key value
  local -a required_keys=(
    PUBLIC_IP
    JWT_SECRET
    DEVICE_LOCK_PIN_PEPPER
    TURN_SECRET
    ORACLE_APP_PASSWORD
    ORACLE_SYS_PASSWORD
  )

  for key in "${required_keys[@]}"; do
    value="$(env_value "$key")"
    [[ -n "$value" ]] || { log "$key is missing or empty in $shared_env"; return 1; }
    case "$value" in
      CHANGE_ME*|060707|a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6|local-device-lock-pepper-change-me-*|nexusteam_turn_secret)
        log "$key still contains a development/default value in $shared_env"
        return 1
        ;;
    esac
  done

  for key in JWT_SECRET DEVICE_LOCK_PIN_PEPPER TURN_SECRET; do
    value="$(env_value "$key")"
    (( ${#value} >= 32 )) || { log "$key must contain at least 32 characters"; return 1; }
  done

  value="$(env_value PUBLIC_IP)"
  [[ "$value" =~ ^([0-9]{1,3}\.){3}[0-9]{1,3}$ ]] || {
    log "PUBLIC_IP must be an IPv4 address"
    return 1
  }
}

validate_environment_file || exit 1

mkdir -- "$release_dir"
tar -xzf "$incoming_archive" -C "$release_dir"
ln -s -- "$shared_env" "$release_dir/.env"

cd "$release_dir"
compose config --quiet || rollback

log "Building the new release while the previous containers are still serving..."
compose build || rollback

if [[ -n "$previous_release" && -f "$previous_release/docker-compose.yaml" ]]; then
  log "Stopping previous release without deleting persistent volumes..."
  stack_transition_started=1
  (cd "$previous_release" && compose down --remove-orphans --timeout 120) || rollback
else
  log "No previous release found; continuing with first deployment"
  stack_transition_started=1
fi

log "Starting new release..."
compose up --detach --no-build --remove-orphans || rollback
wait_for_healthy_container nexusteam_server || rollback
wait_for_healthy_container nexusteam_web || rollback
wait_for_healthy_container nexusteam_gateway || rollback

public_ip="$(env_value PUBLIC_IP)"
if ! compose run --interactive=false -T --rm --no-deps --entrypoint /bin/sh certbot \
  -c "test -s /etc/letsencrypt/live/$public_ip/fullchain.pem"; then
  log "Requesting a trusted short-lived TLS certificate for $public_ip..."
  certbot_arguments=(
    certonly
    --non-interactive
    --agree-tos
    --preferred-profile shortlived
    --webroot
    --webroot-path /var/www/certbot
    --ip-address "$public_ip"
  )
  letsencrypt_email="$(env_value LETSENCRYPT_EMAIL || true)"
  if [[ -n "$letsencrypt_email" ]]; then
    certbot_arguments+=(--email "$letsencrypt_email")
  else
    certbot_arguments+=(--register-unsafely-without-email)
  fi
  compose run --interactive=false -T --rm --no-deps certbot "${certbot_arguments[@]}" || rollback
fi

compose exec -T gateway /usr/local/bin/configure-gateway reload || rollback
docker exec nexusteam_gateway wget --no-check-certificate -qO- \
  "https://127.0.0.1/healthz" >/dev/null || rollback

seeder_exit="$(docker inspect --format '{{.State.ExitCode}}' nexusteam_db_seeder 2>/dev/null || true)"
[[ "$seeder_exit" == "0" ]] || { log "Database seeder exit code is $seeder_exit"; rollback; }

ln -sfn -- "$release_dir" "$deploy_path/current"
rm -f -- "$incoming_archive" "$incoming_env" "$previous_env_backup"

mapfile -t release_directories < <(
  find "$deploy_path/releases" -mindepth 1 -maxdepth 1 -type d -print0 |
    xargs -0 ls -1dt 2>/dev/null || true
)

for ((index = keep_releases; index < ${#release_directories[@]}; index++)); do
  old_release="${release_directories[$index]}"
  [[ "$old_release" == "$deploy_path/releases/"* ]] || continue
  [[ "$old_release" != "$release_dir" ]] || continue
  log "Removing expired release directory: $old_release"
  rm -rf -- "$old_release"
done

log "Deployment completed: $release_id"
compose ps
REMOTE_DEPLOY

log "Deployment successful"
log "Release: $RELEASE_ID"
log "Commit: $COMMIT_SHA"
log "Application: https://$application_host"
