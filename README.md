# Vendor Takeover Lab

A small, working copy of a vendor-built microservices system, for hands-on practice before a takeover:
**.NET 10 microservices (REST + gRPC), Angular, Node.js, SQL Server 2022 + SQL Agent, Redis, MongoDB, MinIO, Traefik, Docker Swarm, Portainer and Azure DevOps pipelines.**

📘 **Start with the guide: [01-How-The-Application-Works.md](01-How-The-Application-Works.md).** It explains every component, then walks you through 15 exercises.

🇵🇰 **Bilkul naye hain? Pehle yeh parhein: [docs/Asaan-Urdu-Guide.md](docs/Asaan-Urdu-Guide.md)**: asaan Roman Urdu mein, tasveeron aur misaalon ke saath.

```
Browser ─► Traefik :8000 ─┬─ /              web-portal (Angular)
                          ├─ /admin         admin-node (Node.js)
                          ├─ /api/orders    orders-api ──gRPC──► customers-api, billing-api
                          ├─ /api/customers customers-api
                          ├─ /api/billing   billing-api
                          └─ /api/documents documents-api ──► MinIO
             SQL Server (SalesDb shared · CustomersDb · DocumentsDb) · Redis · MongoDB · Mailpit
             notification-worker: invoices → email → audit log
```

## Quick start (GitHub Codespaces)

1. **Code → Codespaces → ⋯ → New with options → 4-core → Create codespace.**
   On start, `scripts/bootstrap.sh` sets up Swarm, the network, random passwords (`infra/.env`), the image registry and Portainer.
2. Open **Portainer** (PORTS tab → 9443) and create the admin user within 5 minutes.
3. Build and deploy everything:
   ```bash
   bash scripts/build.sh all
   ```
   ```bash
   bash scripts/deploy.sh
   ```
4. Wait until `docker stack services lab` shows every service running, then open port **8000**.

Stop the Codespace when you're done to save your free hours.

## Scripts

| Command | What it does |
|---|---|
| `bash scripts/build.sh all` | Build and push all images (tags from `infra/versions.env`) |
| `bash scripts/deploy.sh` | Deploy the whole stack from Git |
| `bash scripts/ci.sh <service>` | Local pipeline: test → build → push → rolling update → smoke test |
| `bash scripts/sql-tune.sh [MB]` | Cap SQL Server's cache (runs automatically after deploy) |
| `bash scripts/status.sh` | Nodes, stacks, services, HTTP checks |
| `bash scripts/sql.sh -Q "..."` | Run SQL in the lab's SQL Server |
| `bash scripts/add-workers.sh 2` | Experimental: simulate 2 extra Swarm nodes |
| `bash scripts/plant-deviations.sh` | Plays the vendor: changes things outside Git (exercise E15) |
| `bash scripts/reset.sh [--data]` | Remove the stack (and data) |

Services: `orders-api`, `customers-api`, `billing-api`, `documents-api`, `notification-worker`, `web-portal`, `admin-node`.

## Azure DevOps pipelines

The pipelines in `pipelines/` run on a **self-hosted agent inside your Codespace**, so they can push to the local registry and update the Swarm. This mirrors a vendor running builds on their own machines.

1. **Organisation and project.** Sign in at https://dev.azure.com, create a free organisation, then a project (for example `vendor-takeover-lab`).
2. **Agent pool.** Organization settings → **Agent pools** → **Add pool** → *Self-hosted*, name it **`LabPool`**, tick *Grant access permission to all pipelines*.
3. **Access token for the agent.** User settings → **Personal access tokens** → New token, scope **Agent Pools (Read & manage)**. Keep it ready; you'll paste it into the agent's prompt in the next step.
4. **Install the agent in the Codespace.** In `LabPool` → **New agent** → **Linux** → copy the download link. Then, in the Codespace terminal:
   ```bash
   mkdir -p ~/azagent && cd ~/azagent
   ```
   ```bash
   curl -fsSL "<download link>" | tar -xz
   ```
   ```bash
   ./config.sh
   ```
   Answer the prompts: server URL `https://dev.azure.com/<your-org>`, authentication `PAT`, paste the token, pool `LabPool`, and accept the defaults for the rest. Then start it and leave that terminal open:
   ```bash
   ./run.sh
   ```
5. **Create a pipeline.** Pipelines → **New pipeline** → **GitHub** → choose this repository (authorise the Azure Pipelines app when GitHub asks) → **Existing Azure Pipelines YAML file** → `/pipelines/orders-api.yml` → **Run**.
   Repeat for the other files in `pipelines/`.
6. **Approval gate (optional).** Pipelines → **Environments** → `lab` (created on the first deploy) → **Approvals and checks** → add yourself.

The agent only runs while the Codespace is running and `./run.sh` is open. After a Codespace restart, run `cd ~/azagent && ./run.sh` again.

> No Azure DevOps? `bash scripts/ci.sh <service>` runs the same steps locally.

## Notes

- This is a learning lab. Passwords are random but stored in `infra/.env` in plain text **on purpose**; exercise E11 fixes that.
- Some weaknesses are planted on purpose. The answers are in [docs/instructor-notes.md](docs/instructor-notes.md); try exercise E15 before reading it.
- SQL Server runs as one lightweight instance (Developer edition, to keep SQL Agent) with its memory capped to about 1 GB. Always On is not part of the lab.
