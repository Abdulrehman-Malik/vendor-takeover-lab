#!/usr/bin/env bash
# Keeps SQL Server light: caps its cache ("max server memory") so the instance uses about
# 1 GB instead of most of the 2 GB it is allowed. The setting is stored in the master
# database on the sqldata volume, so it survives restarts. Safe to re-run.
# Runs automatically at the end of scripts/deploy.sh.
#   bash scripts/sql-tune.sh        # 768 MB cache (default)
#   bash scripts/sql-tune.sh 1024   # a different cap
source "$(dirname "$0")/lib.sh"

max_mb="${1:-768}"

step "Capping SQL Server memory (max server memory = $max_mb MB)"
for _ in $(seq 1 40); do
  if bash "$ROOT/scripts/sql.sh" -Q "SELECT 1" >/dev/null 2>&1; then
    bash "$ROOT/scripts/sql.sh" -Q "SET NOCOUNT ON;
      EXEC sp_configure 'show advanced options', 1; RECONFIGURE;
      EXEC sp_configure 'max server memory (MB)', $max_mb; RECONFIGURE;" >/dev/null
    echo "Done. Check real usage with: docker stats --no-stream"
    exit 0
  fi
  sleep 5
done
echo "SQL Server isn't accepting connections yet. Run this again in a minute: bash scripts/sql-tune.sh"
