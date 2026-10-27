#!/usr/bin/env bash
# Run sqlcmd inside the SQL Server container.
#   bash scripts/sql.sh -Q "SELECT name FROM sys.databases"
#   bash scripts/sql.sh -d SalesDb -Q "SELECT * FROM orders.Orders"
#   bash scripts/sql.sh -i db/agent-jobs/ArchiveOldOrders.sql
source "$(dirname "$0")/lib.sh"

container="$(docker ps -q -f "name=${STACK}_sqlserver" | head -n1)"
[[ -n "$container" ]] || fail "SQL Server container is not running."

args=()
while [[ $# -gt 0 ]]; do
  if [[ "$1" == "-i" ]]; then
    # Copy a local script file into the container, then run it there.
    docker cp "$2" "$container:/tmp/script.sql" >/dev/null
    args+=(-i /tmp/script.sql)
    shift 2
  else
    args+=("$1")
    shift
  fi
done

docker exec -i "$container" /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa \
  -P "$(envval MSSQL_SA_PASSWORD)" "${args[@]}"
