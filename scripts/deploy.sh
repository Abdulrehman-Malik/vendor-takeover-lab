#!/usr/bin/env bash
# Deploy (or redeploy) the whole stack from Git: infra/stack.yml + infra/versions.env.
# Redeploying overwrites anything that was changed by hand in Portainer or with docker service update.
source "$(dirname "$0")/lib.sh"

[[ -f "$ROOT/infra/.env" ]] || fail "infra/.env is missing. Run: bash scripts/bootstrap.sh"

load_versions
step "docker stack deploy -c infra/stack.yml $STACK"
docker stack deploy --with-registry-auth -c "$ROOT/infra/stack.yml" "$STACK"

bash "$ROOT/scripts/sql-tune.sh"

step "Services (replicas fill in as containers start)"
docker stack services "$STACK"
echo
echo "Watch progress with: watch docker stack services $STACK"
echo "Then open port 8000 (portal), 8000/admin (health), 8088 (Traefik), 9443 (Portainer)."
