#!/usr/bin/env bash
set -euo pipefail
# Après connexion via navigateur, copier la valeur du cookie seulement dans une variable locale.
# Ne jamais coller un jeton de session dans des journaux ni un dépôt Git.
if [ -z "${SESSION_TOKEN:-}" ]; then
  echo 'Définir SESSION_TOKEN temporairement depuis un cookie local (ne pas le publier).'; exit 1
fi
HASH=$(printf '%s' "$SESSION_TOKEN" | sha256sum | cut -d' ' -f1)
docker compose exec -T redis redis-cli TYPE "session:$HASH"
docker compose exec -T redis redis-cli TTL "session:$HASH"
unset SESSION_TOKEN
