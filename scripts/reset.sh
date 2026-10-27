#!/usr/bin/env bash
# Remove the lab stack. With --data, also delete SQL Server, MongoDB and MinIO data.
#   bash scripts/reset.sh          # keep data
#   bash scripts/reset.sh --data   # start completely fresh
source "$(dirname "$0")/lib.sh"

step "Removing stack '$STACK'"
docker stack rm "$STACK" || true

if [[ "${1:-}" == "--data" ]]; then
  step "Waiting for containers to stop, then deleting data volumes"
  for _ in $(seq 1 30); do
    [[ -z "$(docker ps -q -f "label=com.docker.stack.namespace=$STACK")" ]] && break
    sleep 2
  done
  docker volume rm "${STACK}_sqldata" "${STACK}_mongodata" "${STACK}_miniodata" 2>/dev/null || true
  git -C "$ROOT" checkout -- infra/versions.env 2>/dev/null || true
fi

echo "Done. Redeploy with: bash scripts/deploy.sh"
