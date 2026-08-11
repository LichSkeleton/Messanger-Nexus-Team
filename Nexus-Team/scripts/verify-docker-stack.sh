#!/usr/bin/env bash

set -Eeuo pipefail

expected_arch="${EXPECTED_ARCH:-}"
web_port="${WEB_PORT:-8080}"
server_port="${SERVER_PORT:-5251}"
oracle_user="${ORACLE_APP_USER:-nexusteam_admin}"
oracle_password="${ORACLE_APP_PASSWORD:-060707}"
oracle_service="${ORACLE_SERVICE:-FREEPDB1}"
mongo_database="${MONGO_DATABASE:-NexusTeam}"

echo "Verifying DB seeder..."
seeder_exit_code="$(docker inspect nexusteam_db_seeder --format='{{.State.ExitCode}}')"
if [[ "$seeder_exit_code" != "0" ]]; then
  echo "Database seeder failed with exit code $seeder_exit_code"
  docker logs nexusteam_db_seeder
  exit 1
fi

if [[ -n "$expected_arch" ]]; then
  echo "Verifying runtime architecture..."
  runtime_arch="$(docker exec nexusteam_server uname -m)"
  case "$expected_arch" in
    amd64)
      if [[ "$runtime_arch" != "x86_64" && "$runtime_arch" != "amd64" ]]; then
        echo "Runtime architecture mismatch: expected amd64, got $runtime_arch"
        exit 1
      fi
      ;;
    arm64)
      if [[ "$runtime_arch" != "aarch64" && "$runtime_arch" != "arm64" ]]; then
        echo "Runtime architecture mismatch: expected arm64, got $runtime_arch"
        exit 1
      fi
      ;;
    *)
      echo "Unsupported EXPECTED_ARCH value: $expected_arch"
      exit 1
      ;;
  esac
  echo "Runtime architecture is $runtime_arch"
fi

echo "Verifying MongoDB collections and indexes..."
docker exec -e VERIFY_MONGO_DATABASE="$mongo_database" \
  nexusteam_mongos mongosh --quiet --eval '
  const appDb = db.getSiblingDB(process.env.VERIFY_MONGO_DATABASE);
  const collections = appDb.getCollectionNames();
  const required = ["chats", "messages", "attachments", "user_preferences"];

  for (const collection of required) {
    if (!collections.includes(collection)) {
      throw new Error(`Collection ${collection} not found`);
    }
  }

  const chatsIndexes = appDb.chats.getIndexes();
  if (chatsIndexes.length < 5) {
    throw new Error(`Expected at least 5 chats indexes, found ${chatsIndexes.length}`);
  }

  print(`Collections: ${collections.join(", ")}`);
  print(`Chats indexes: ${chatsIndexes.length}`);
'

echo "Verifying MongoDB sharding..."
docker exec nexusteam_mongos mongosh --quiet --eval '
  const shards = db.getSiblingDB("config").shards.find().toArray();
  if (shards.length < 2) {
    throw new Error(`Expected at least 2 shards, found ${shards.length}`);
  }
  print(`Shards: ${shards.map((shard) => shard._id).join(", ")}`);
'

echo "Verifying Oracle tables and demo users..."
oracle_output="$({
  docker exec \
    -e VERIFY_ORACLE_USER="$oracle_user" \
    -e VERIFY_ORACLE_PASSWORD="$oracle_password" \
    -e VERIFY_ORACLE_SERVICE="$oracle_service" \
    nexusteam_oracle sh -c '
      printf "%s\n" "SET HEADING OFF FEEDBACK OFF PAGESIZE 0" \
        "SELECT username FROM users ORDER BY username;" \
        "EXIT;" |
        sqlplus -s "$VERIFY_ORACLE_USER/$VERIFY_ORACLE_PASSWORD@//localhost:1521/$VERIFY_ORACLE_SERVICE"
    '
} 2>&1)"
echo "$oracle_output"

for demo_user in Vlad Sofia Hakan Anna; do
  if ! grep -q "$demo_user" <<<"$oracle_output"; then
    echo "Demo user $demo_user not found in Oracle"
    exit 1
  fi
done

echo "Verifying Redis..."
docker exec nexusteam_redis redis-cli ping | grep -q PONG
docker exec nexusteam_redis redis-cli INFO server | grep -q redis_version

echo "Verifying web client and API..."
curl -fsS --retry 30 --retry-delay 5 --retry-all-errors \
  "http://localhost:${web_port}/healthz" >/dev/null
curl -fsS "http://localhost:${web_port}/" >/dev/null
curl -fsS "http://localhost:${server_port}/health" | grep -q Healthy

login_status="$(curl -sS -o /dev/null -w '%{http_code}' -X POST \
  -H 'Content-Type: application/json' \
  -d '{"usernameOrEmail":"no-such-user","password":"x"}' \
  "http://localhost:${web_port}/api/auth/login")"

if [[ "$login_status" != "401" ]]; then
  echo "Expected 401 from /api/auth/login, got $login_status"
  docker compose logs server --tail=50
  exit 1
fi

echo "Docker stack verification completed successfully."
