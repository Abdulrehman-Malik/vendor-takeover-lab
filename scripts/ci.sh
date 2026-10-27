#!/usr/bin/env bash
# The same steps as the Azure DevOps pipeline, run locally:
#   test -> build image -> push to registry -> rolling update -> smoke test -> record the tag
#
#   bash scripts/ci.sh orders-api
source "$(dirname "$0")/lib.sh"

svc="${1:?Usage: bash scripts/ci.sh <service>   (one of: $ALL_SERVICES)}"
require_service "$svc"
cd "$ROOT"

# Unique tag: git commit + time, so re-running without a new commit still gives a new tag.
tag="$(git rev-parse --short HEAD 2>/dev/null || echo local)-$(date +%H%M%S)"

declare -A TESTS=([orders-api]=tests/Orders.Tests)
declare -A SMOKE=(
  [orders-api]=/api/orders/version
  [customers-api]=/api/customers/version
  [billing-api]=/api/billing/version
  [documents-api]=/api/documents
  [web-portal]=/
  [admin-node]=/admin/api/status
)

if [[ -n "${TESTS[$svc]:-}" ]]; then
  step "1/4 Unit tests: ${TESTS[$svc]}  (quality gate: a failure stops here)"
  dotnet test "${TESTS[$svc]}" -c Release
else
  step "1/4 Unit tests: none for $svc"
fi

step "2/4 Build image $REGISTRY/$svc:$tag"
docker build -f "${DOCKERFILE[$svc]}" -t "$REGISTRY/$svc:$tag" .

step "3/4 Push to registry"
docker push "$REGISTRY/$svc:$tag"

step "4/4 Deploy"
bash "$ROOT/scripts/deploy-service.sh" "$svc" "$tag" "${SMOKE[$svc]:-}"

set_version "${TAGVAR[$svc]}" "$tag"
step "Done. ${TAGVAR[$svc]}=$tag recorded in infra/versions.env (commit it so Git matches what is running)."
