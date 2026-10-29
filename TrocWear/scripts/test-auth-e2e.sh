#!/usr/bin/env bash
set -euo pipefail
BASE="${BASE_URL:-http://localhost:5080}"
DIR="$(mktemp -d)"; trap 'rm -rf "$DIR"' EXIT
EMAIL="e2e-$(date +%s)-$$@example.test"
PASSWORD='a-good-password-for-tests'
EMAIL_UP=$(printf '%s' "$EMAIL" | tr '[:lower:]' '[:upper:]')
HEADER='X-TrocWear-Request: 1'
status=$(curl -sS -o "$DIR/registered" -w '%{http_code}' -H "$HEADER" -H 'Content-Type: application/json' \
  -d "{\"pseudonyme\":\"E2ETest\",\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}" "$BASE/api/auth/register")
[ "$status" = 201 ] || { cat "$DIR/registered"; echo 'FAIL register:' "$status"; exit 1; }
status=$(curl -sS -o "$DIR/duplicate" -w '%{http_code}' -H "$HEADER" -H 'Content-Type: application/json' \
  -d "{\"pseudonyme\":\"Duplicate\",\"email\":\"$EMAIL_UP\",\"password\":\"$PASSWORD\"}" "$BASE/api/auth/register")
[ "$status" = 409 ] || { echo 'FAIL duplicate:' "$status"; exit 1; }
status=$(curl -sS -c "$DIR/cookies" -o "$DIR/login" -w '%{http_code}' -H "$HEADER" -H 'Content-Type: application/json' \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}" "$BASE/api/auth/login")
[ "$status" = 200 ] || { echo 'FAIL login:' "$status"; exit 1; }
# Vérification du stockage NoSQL : nom de clé = SHA-256 du token, expiration Redis.
if command -v docker >/dev/null 2>&1 && docker compose ps --services --status running 2>/dev/null | grep -qx redis; then
  TOKEN=$(awk '$6=="trocwear.sid" {print $7}' "$DIR/cookies")
  HASH=$(printf '%s' "$TOKEN" | sha256sum | cut -d' ' -f1)
  KEY="session:$HASH"
  redis_ttl=$(docker compose exec -T redis redis-cli TTL "$KEY" | tr -d '\r')
  if [ "$redis_ttl" -le 0 ] || [ "$redis_ttl" -gt 3600 ]; then
    echo "FAIL Redis TTL:" "$redis_ttl"; exit 1
  fi
  unset TOKEN
  echo "PASS Redis session present, TTL=$redis_ttl seconds"
else
  echo 'SKIP Redis direct: Docker Compose Redis not running or Docker absent'
fi
status=$(curl -sS -b "$DIR/cookies" -o "$DIR/me" -w '%{http_code}' "$BASE/api/auth/me")
[ "$status" = 200 ] || { echo 'FAIL /me:' "$status"; exit 1; }
python3 - "$DIR/me" "$EMAIL" <<'PYTH'
import json,sys
assert json.load(open(sys.argv[1]))['email']==sys.argv[2]
PYTH
# Validation SQL : compte présent, secret non stocké en clair, événement outbox créé.
if command -v docker >/dev/null 2>&1 && docker compose ps --services --status running 2>/dev/null | grep -qx postgres; then
  pg_user="${POSTGRES_USER:-trocwear}"; pg_db="${POSTGRES_DB:-trocwear_auth}"
  rows=$(docker compose exec -T postgres psql -U "$pg_user" -d "$pg_db" -tAc \
    "SELECT COUNT(*) FROM users WHERE \"Email\"='$EMAIL' AND \"PasswordHash\" <> '$PASSWORD';" | tr -d '[:space:]')
  [ "$rows" = 1 ] || { echo 'FAIL PostgreSQL account or password hash'; exit 1; }
  events=$(docker compose exec -T postgres psql -U "$pg_user" -d "$pg_db" -tAc \
    "SELECT COUNT(*) FROM outbox_messages WHERE \"EventType\"='UtilisateurCree';" | tr -d '[:space:]')
  [ "$events" -ge 1 ] || { echo 'FAIL PostgreSQL outbox'; exit 1; }
  echo 'PASS PostgreSQL account hashed + public event in outbox'
else
  echo 'SKIP PostgreSQL direct: Docker Compose PostgreSQL not running or Docker absent'
fi
status=$(curl -sS -b "$DIR/cookies" -o /dev/null -w '%{http_code}' -H "$HEADER" \
 -H 'Content-Type: application/json' -d '{"pseudonyme":"UpdatedE2E","versionAttendue":1}' \
 -X PATCH "$BASE/api/auth/profile")
[ "$status" = 200 ] || { echo 'FAIL profile:' "$status"; exit 1; }
status=$(curl -sS -b "$DIR/cookies" -o /dev/null -w '%{http_code}' -H "$HEADER" \
 -X POST "$BASE/api/auth/logout")
[ "$status" = 204 ] || { echo 'FAIL logout:' "$status"; exit 1; }
if command -v docker >/dev/null 2>&1 && docker compose ps --services --status running 2>/dev/null | grep -qx redis; then
  redis_exists=$(docker compose exec -T redis redis-cli EXISTS "$KEY" | tr -d '\r')
  [ "$redis_exists" = 0 ] || { echo 'FAIL Redis session not removed'; exit 1; }
  echo 'PASS Redis DEL session'
fi
status=$(curl -sS -b "$DIR/cookies" -o /dev/null -w '%{http_code}' "$BASE/api/auth/me")
[ "$status" = 401 ] || { echo 'FAIL session revocation:' "$status"; exit 1; }
status=$(curl -sS -o /dev/null -w '%{http_code}' "$BASE/api/auth/me")
[ "$status" = 401 ] || { echo 'FAIL missing session:' "$status"; exit 1; }
status=$(curl -sS -H 'Cookie: trocwear.sid=invalid_session' -o /dev/null -w '%{http_code}' "$BASE/api/auth/me")
[ "$status" = 401 ] || { echo 'FAIL invalid session:' "$status"; exit 1; }
echo 'PASS: register, case-insensitive duplicate, login, me, profile, logout, Redis DEL, invalid/missing session'

