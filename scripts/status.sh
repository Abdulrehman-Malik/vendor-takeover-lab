#!/usr/bin/env bash
# Quick health overview of the lab.
source "$(dirname "$0")/lib.sh"

step "Swarm nodes"
docker node ls

step "Stacks"
docker stack ls

step "Services in '$STACK'"
docker stack services "$STACK" 2>/dev/null || echo "Stack '$STACK' is not deployed yet."

step "Through Traefik (http://localhost:8000)"
for path in /api/orders/version /api/customers/version /api/billing/version /api/documents /admin/api/status; do
  code="$(curl -s -o /dev/null -w '%{http_code}' "http://localhost:8000$path" || true)"
  printf '  %-28s %s\n' "$path" "$code"
done
