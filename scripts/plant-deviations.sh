#!/usr/bin/env bash
# Plays the role of the vendor: makes the kind of changes that never reach Git.
# Run it once the full stack is healthy, then try to find everything it did (exercise E15).
# Don't read the rest of this file until you've done the audit. The answers are in docs/instructor-notes.md.
source "$(dirname "$0")/lib.sh"
cd "$ROOT"

step "The vendor is making some changes..."

# 1. Live config edited outside Git (what a Portainer UI edit does)
docker service update --quiet --replicas 3 --env-add FEATURE_X=true "${STACK}_orders-api" >/dev/null
docker service update --quiet --env-add Logging__LogLevel__Default=Debug "${STACK}_customers-api" >/dev/null

# 2. A schema change applied by hand, not as a migration
bash scripts/sql.sh -i db/manual-scripts/2026-09-14_hotfix_add_priority.sql >/dev/null

# 3. A SQL Agent job created directly on the server; no script in db/agent-jobs
bash scripts/sql.sh -Q "
IF NOT EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = N'NightlyInvoiceCleanup')
BEGIN
  EXEC msdb.dbo.sp_add_job @job_name = N'NightlyInvoiceCleanup';
  EXEC msdb.dbo.sp_add_jobstep @job_name = N'NightlyInvoiceCleanup', @step_name = N'Cleanup',
       @subsystem = N'TSQL', @database_name = N'SalesDb',
       @command = N'UPDATE billing.Invoices SET EmailSent = 1 WHERE CreatedAt < DATEADD(day, -30, SYSUTCDATETIME());';
  EXEC msdb.dbo.sp_add_jobschedule @job_name = N'NightlyInvoiceCleanup', @name = N'Nightly0200',
       @freq_type = 4, @freq_interval = 1, @active_start_time = 020000;
  EXEC msdb.dbo.sp_add_jobserver @job_name = N'NightlyInvoiceCleanup';
END" >/dev/null

# 4. An image built and pushed from someone's laptop, bypassing the pipeline
docker build -q -f src/billing-api/Dockerfile -t "$REGISTRY/billing-api:hotfix-laptop" . >/dev/null
docker push -q "$REGISTRY/billing-api:hotfix-laptop" >/dev/null
docker service update --quiet --with-registry-auth --image "$REGISTRY/billing-api:hotfix-laptop" "${STACK}_billing-api" >/dev/null

# 5. A maintenance script left on the "server", holding a copy of a password
mkdir -p /tmp/vendor-home
cat > /tmp/vendor-home/backup-sales.sh <<EOF
#!/bin/sh
# nightly backup, runs from cron on the swarm manager. do not delete!!
sqlcmd -S sqlserver -U sa -P '$(envval MSSQL_SA_PASSWORD)' -Q "BACKUP DATABASE SalesDb TO DISK='/var/opt/mssql/backup/sales.bak'"
EOF

step "Done. Several things were changed outside Git. Start your audit (guide exercise E15)."
