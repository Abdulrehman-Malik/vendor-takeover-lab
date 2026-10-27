#!/usr/bin/env bash
# Shared helpers, sourced by the other scripts.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REGISTRY="${REGISTRY:-localhost:5000}"
STACK="lab"

# service name -> Dockerfile, and service name -> tag variable in infra/versions.env
declare -A DOCKERFILE=(
  [orders-api]=src/orders-api/Dockerfile
  [customers-api]=src/customers-api/Dockerfile
  [billing-api]=src/billing-api/Dockerfile
  [documents-api]=src/documents-api/Dockerfile
  [notification-worker]=src/notification-worker/Dockerfile
  [web-portal]=src/web-portal/Dockerfile
  [admin-node]=src/admin-node/Dockerfile
)
declare -A TAGVAR=(
  [orders-api]=ORDERS_TAG
  [customers-api]=CUSTOMERS_TAG
  [billing-api]=BILLING_TAG
  [documents-api]=DOCUMENTS_TAG
  [notification-worker]=WORKER_TAG
  [web-portal]=PORTAL_TAG
  [admin-node]=ADMIN_TAG
)
ALL_SERVICES="orders-api customers-api billing-api documents-api notification-worker web-portal admin-node"

step() { printf '\n\033[1;34m==> %s\033[0m\n' "$*"; }
fail() { printf '\033[1;31mERROR: %s\033[0m\n' "$*" >&2; exit 1; }

require_service() {
  [[ -n "${DOCKERFILE[$1]:-}" ]] || fail "Unknown service '$1'. Choose one of: $ALL_SERVICES"
}

# Read one value from infra/.env. (The file can't be "source"d: connection strings contain ';'.)
envval() {
  grep -E "^$1=" "$ROOT/infra/.env" | head -n1 | cut -d= -f2-
}

load_versions() {
  set -a
  # shellcheck disable=SC1091
  source "$ROOT/infra/versions.env"
  set +a
}

set_version() {
  local var="$1" tag="$2"
  sed -i "s/^${var}=.*/${var}=${tag}/" "$ROOT/infra/versions.env"
}

wait_for_docker() {
  for _ in $(seq 1 60); do
    docker info >/dev/null 2>&1 && return 0
    sleep 2
  done
  fail "Docker is not running."
}
