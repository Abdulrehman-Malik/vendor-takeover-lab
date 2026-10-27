#!/usr/bin/env bash
# EXPERIMENTAL: simulate a multi-node Swarm by running extra Docker engines as containers
# ("Docker-in-Docker") and joining them as worker nodes.
#   bash scripts/add-workers.sh 2       # add lab-worker-1 and lab-worker-2
#   bash scripts/add-workers.sh remove  # remove them again
#
# Data services (SQL Server, MongoDB, MinIO, registry, Portainer) are pinned to the manager,
# so only the application services move between nodes.
source "$(dirname "$0")/lib.sh"

if [[ "${1:-}" == "remove" ]]; then
  for name in $(docker ps -a --format '{{.Names}}' | grep '^lab-worker-' || true); do
    step "Removing $name"
    docker exec "$name" docker swarm leave --force >/dev/null 2>&1 || true
    docker rm -f "$name" >/dev/null
    docker node rm --force "$name" >/dev/null 2>&1 || true
  done
  docker node ls
  exit 0
fi

count="${1:-2}"
manager_addr="$(docker info --format '{{.Swarm.NodeAddr}}')"
token="$(docker swarm join-token -q worker)"

for i in $(seq 1 "$count"); do
  name="lab-worker-$i"
  if docker ps -a --format '{{.Names}}' | grep -qx "$name"; then
    echo "$name already exists, skipping."
    continue
  fi

  step "Starting $name"
  docker run -d --privileged --name "$name" --hostname "$name" docker:28-dind >/dev/null
  for _ in $(seq 1 30); do
    docker exec "$name" docker info >/dev/null 2>&1 && break
    sleep 2
  done

  step "Joining $name to the Swarm at $manager_addr"
  docker exec "$name" docker swarm join --token "$token" "$manager_addr:2377"
done

step "Nodes"
docker node ls
echo
echo "Spread the app services out with: docker service update --force ${STACK}_orders-api"
echo "See where tasks run:              docker service ps ${STACK}_orders-api"
echo "Drain a node:                     docker node update --availability drain lab-worker-1"
