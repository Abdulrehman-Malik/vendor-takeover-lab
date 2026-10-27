#!/usr/bin/env bash
# Rolling update of ONE service to a new image tag, then a smoke test.
# Used by scripts/ci.sh and by the Azure DevOps pipelines.
#   bash scripts/deploy-service.sh orders-api 42 /api/orders/version
source "$(dirname "$0")/lib.sh"

svc="${1:?service name}"
tag="${2:?image tag}"
smoke="${3:-}"
require_service "$svc"

step "Rolling update: ${STACK}_${svc} -> $REGISTRY/$svc:$tag"
if ! docker service update --with-registry-auth --image "$REGISTRY/$svc:$tag" "${STACK}_${svc}"; then
  echo "docker service update reported a failure (see above). Checking what is running now..."
fi

step "Tasks after the update"
docker service ps "${STACK}_${svc}" --format 'table {{.Name}}\t{{.Image}}\t{{.CurrentState}}\t{{.Error}}' | head -n 8

current="$(docker service inspect "${STACK}_${svc}" --format '{{.Spec.TaskTemplate.ContainerSpec.Image}}')"
if [[ "$current" != "$REGISTRY/$svc:$tag"* ]]; then
  fail "Swarm is running '$current', not tag $tag. The update was rolled back (check the health of the new version)."
fi

if [[ -n "$smoke" ]]; then
  step "Smoke test: GET http://localhost:8000$smoke"
  for _ in $(seq 1 15); do
    if curl -fsS "http://localhost:8000$smoke"; then echo; exit 0; fi
    sleep 2
  done
  fail "Smoke test failed."
fi
