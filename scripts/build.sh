#!/usr/bin/env bash
# Build and push images to the local registry.
#   bash scripts/build.sh all            # every service, using the tags in infra/versions.env
#   bash scripts/build.sh orders-api 7   # one service with an explicit tag
source "$(dirname "$0")/lib.sh"

target="${1:-all}"
cd "$ROOT"
load_versions

build_one() {
  local svc="$1" tag="$2"
  local image="$REGISTRY/$svc:$tag"
  step "Build $image"
  docker build -f "${DOCKERFILE[$svc]}" -t "$image" .
  step "Push $image"
  docker push "$image"
}

if [[ "$target" == "all" ]]; then
  for svc in $ALL_SERVICES; do
    var="${TAGVAR[$svc]}"
    build_one "$svc" "${!var}"
  done
else
  require_service "$target"
  var="${TAGVAR[$target]}"
  build_one "$target" "${2:-${!var}}"
fi

step "Images now in the registry"
curl -fs "http://localhost:5000/v2/_catalog"; echo
