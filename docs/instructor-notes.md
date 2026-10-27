# Instructor Notes: Planted Deviations (spoilers)

> Do exercise E15 **before** reading this. These are the answers.

## Built into the repo from the start

| # | Deviation zone | Where | How to spot it |
|---|---|---|---|
| A | Secrets in `.env` | `infra/stack.yml` loads `infra/.env` into **every** service with `env_file` | `docker stack deploy` copies `env_file` values into the service definition, so `docker service inspect lab_orders-api --format '{{json .Spec.TaskTemplate.ContainerSpec.Env}}'` prints the SA password in plain text. So does Portainer's service page. Even `notification-worker`, which never needs MinIO, gets the MinIO password |
| B | Untested rollback | `billing-api` has **no** healthcheck, `update_config` or `rollback_config` | Compare its block in `stack.yml` with `orders-api`. A broken billing release would never roll back on its own |
| C | Missing pipeline | `pipelines/` has a file for every service **except billing-api** | `ls pipelines/`. Billing can only be released by hand |
| D | Shared database with hidden consumer | `notification-worker` reads and updates `billing.Invoices` with raw SQL, but `billing-api` owns that schema | Search the code for `billing.Invoices`. Renaming `EmailSent` in billing-api would break the worker with no compile error |
| E | Pipeline deploys don't update Git | Pipelines run `docker service update` with `$(Build.BuildId)` but never write the tag to `infra/versions.env` | After a pipeline deploy, `bash scripts/deploy.sh` rolls the service **back** to the tag in Git |

## Added by `scripts/plant-deviations.sh`

| # | Deviation zone | What it did | How to spot it |
|---|---|---|---|
| 1 | Portainer drift | `orders-api`: 2 → 3 replicas, added `FEATURE_X=true`. `customers-api`: added `Logging__LogLevel__Default=Debug` | `docker service inspect --pretty lab_orders-api` vs. `infra/stack.yml`. Portainer → Services shows the same |
| 2 | Manual DB script | Added column `orders.Orders.Priority` by hand | `bash scripts/sql.sh -d SalesDb -Q "SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('orders.Orders')"` vs. the `Order` class and `dotnet ef migrations list --project src/orders-api` |
| 3 | SQL Agent job not in Git | Job `NightlyInvoiceCleanup` | `bash scripts/sql.sh -Q "SELECT name FROM msdb.dbo.sysjobs"` vs. `ls db/agent-jobs` |
| 4 | Manual image push | `billing-api` now runs tag `hotfix-laptop`, which no pipeline built | `docker service inspect lab_billing-api --format '{{.Spec.TaskTemplate.ContainerSpec.Image}}'` vs. `infra/versions.env`. `curl http://localhost:5000/v2/billing-api/tags/list` |
| 5 | Script with hardcoded password on the host | `/tmp/vendor-home/backup-sales.sh` (stands in for `/home/lmkr/...`) | `ls -la /tmp/vendor-home` and `grep -r -- "-P " /tmp/vendor-home`. Also note it backs up to a folder nobody mounted |

## Putting it right

1. Decide which live values are correct, and write them into `infra/stack.yml` / `infra/versions.env`. Then `bash scripts/deploy.sh`.
2. Turn the manual column into a real migration (`dotnet ef migrations add AddPriority`, matching the existing column), or drop it.
3. Script `NightlyInvoiceCleanup` into `db/agent-jobs/`.
4. Rebuild billing-api through a pipeline; add `pipelines/billing-api.yml`.
5. Move connection strings into Swarm secrets (exercise E11) and delete the host script.
