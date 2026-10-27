# How the Application Works — A Beginner's Guide

> **Who this is for:** a developer joining the team who has never seen this system before.
> **Goal:** after reading this, you should be able to explain how a request travels through the system, where data lives, and how code gets from a laptop to production.
>
> **How to use it:**
> - **Sections 1–7** explain the system: every component, the CI/CD pipeline, and how they all talk.
> - **Section 8** starts the lab in GitHub Codespaces (nothing to install on your laptop).
> - **Section 9** lets you try **each component on its own** with a few commands.
> - **Section 10** has **end-to-end exercises** on the lab system: change code, deploy, break things, roll back, and audit.
>
> The lab system in this repo is a small, working copy of the real architecture: 4 .NET services + a worker, an Angular portal, a Node.js admin app, SQL Server, Redis, MongoDB, MinIO, Traefik, Portainer and Docker Swarm, with Azure DevOps pipelines.

---

## 1. The Big Picture in One Minute

The application is a **microservices system**. Instead of one giant program, it is split into many small programs, and each one does one job.

| What | How many | Built with |
|---|---|---|
| Backend services (the "brains") | 27 | .NET Core (C#) |
| Frontend websites (what users see) | 5 | Angular (TypeScript) |
| Admin tool | 1 | Node.js |
| Databases | 21 | SQL Server 2022 |
| Code repositories | 33 | Azure DevOps |

Everything runs inside **Docker containers**, which are managed by **Docker Swarm**.

### A restaurant analogy

Keep this picture in your head. Every component maps to something in a restaurant:

| Restaurant | Our system | Its job |
|---|---|---|
| Front door and host who seats you | **Reverse proxy** (Nginx/Traefik) | Receives every visitor and sends them to the right place |
| Menu and waiter | **Angular frontend** | What the customer sees and interacts with |
| Kitchen stations (grill, salad, desserts) | **.NET microservices** | Each one does one specific job |
| Kitchen intercom between stations | **gRPC** | Fast internal talk between services |
| Order slips from waiter to kitchen | **REST APIs** | How the frontend asks the backend for things |
| The pantry and recipe books | **SQL Server** | Stores the permanent, structured data |
| The walk-in storage room | **MinIO** | Stores files (PDFs, images, uploads) |
| Prep counter with items ready to grab | **Redis** | A fast cache for things needed often |
| A notebook for odd, unstructured notes | **MongoDB** | Stores flexible, document-shaped data |
| Pre-packed meal boxes | **Docker containers** | Each service, packed with everything it needs |
| The restaurant manager | **Docker Swarm** | Decides which kitchen runs which box and replaces broken ones |
| The manager's dashboard | **Portainer** | A web screen for seeing and controlling Swarm |
| The central warehouse of meal boxes | **Azure Container Registry (ACR)** | Stores every version of every container image |
| The factory that packs the meal boxes | **Azure DevOps Pipelines** | Builds code into images automatically |
| The locked safe | **Azure Key Vault** | Holds passwords and secrets |
| The ID check at the door | **IDP (Identity Provider)** | Confirms who a user is (login) |
| Card machine | **Payment gateway** | Takes payments |
| Postal service | **SMTP** | Sends emails |
| Night shift cleaning crew | **SQL Agent jobs and background workers** | Run scheduled tasks automatically |

---

## 2. The Architecture Diagram

```
                         ┌──────────────┐
                         │    USER      │  (web browser)
                         └──────┬───────┘
                                │ HTTPS
                                ▼
                  ┌───────────────────────────┐
                  │  REVERSE PROXY            │  Nginx or Traefik
                  │  (SSL certificates here)  │
                  └─────┬───────────┬─────────┘
                        │           │
           ┌────────────┘           └─────────────┐
           ▼                                      ▼
  ┌──────────────────┐                  ┌──────────────────┐
  │ 5 ANGULAR APPS   │                  │ NODE.JS ADMIN    │
  │ (frontend)       │                  │                  │
  └────────┬─────────┘                  └────────┬─────────┘
           │ REST (JSON over HTTP)               │
           ▼                                     ▼
  ┌─────────────────────────────────────────────────────────┐
  │           27 .NET CORE MICROSERVICES                    │
  │                                                         │
  │   [Orders] ◄──gRPC──► [Customers] ◄──gRPC──► [Billing]  │
  │      ▲                                          │       │
  │      └──────────────gRPC────────────────────────┘       │
  └───┬──────────┬───────────┬───────────┬──────────┬───────┘
      │          │           │           │          │
      ▼          ▼           ▼           ▼          ▼
 ┌─────────┐ ┌───────┐ ┌──────────┐ ┌────────┐ ┌──────────────┐
 │SQL      │ │ MinIO │ │  Redis   │ │MongoDB │ │ IDP / Payment│
 │Server   │ │(files)│ │ (cache)  │ │ (docs) │ │ / SMTP       │
 │21 DBs   │ └───────┘ └──────────┘ └────────┘ └──────────────┘
 │Always On│
 └─────────┘

 ════════════════  everything above runs inside  ════════════════
              DOCKER CONTAINERS managed by DOCKER SWARM
                   (viewed and controlled through PORTAINER)
```

> The service names `Orders`, `Customers` and `Billing` are **made-up examples** (the lab uses them too). The real system has 27 services with their own names.

---

## 3. Meet Each Component

Each section answers four questions:
- **What is it?**
- **Why is it here?**
- **How does it talk to others?**
- **What to remember**

---

### 3.1 Angular Frontends (5 apps)

**What is it?** Angular is a framework for building websites that run in the browser. The user downloads HTML, CSS and JavaScript, and the page runs on their computer.

**Why is it here?** It is the screen the user clicks on. There are 5 because different users (customers, staff, managers, and so on) probably have different portals.

**How does it talk?** It **never touches the database directly**. When it needs data, it sends a **REST** request to a backend service:

```
GET  https://app.company.com/api/orders/123
→ Response: { "id": 123, "status": "Delivered", "amount": 4500 }
```

**What to remember:** The frontend is just a "display". All business rules live in the backend.

---

### 3.2 Node.js Admin App

**What is it?** A small web app written in JavaScript that runs on the server.

**Why is it here?** It is an internal tool for administrators (managing settings, users, and so on).

**How does it talk?** Like the Angular apps, it calls backend services over REST.

---

### 3.3 .NET Core Microservices (27 services)

**What is it?** Small C# web programs. Each one is a separate project with its own repository and its own container.

**Why so many?** Each service owns one area of the business. Splitting them means:
- one team can change `Billing` without touching `Orders`
- if `Billing` crashes, `Orders` can keep working
- a busy service can be scaled up (run more copies) on its own

**How does it talk?** In two ways:

| Style | Used between | Format | Analogy |
|---|---|---|---|
| **REST** | Frontend → Backend | JSON over HTTP | Writing a letter: readable, a little slow |
| **gRPC** | Backend → Backend | Binary (Protobuf) over HTTP/2 | A phone call: fast and compact, but both sides must agree on the "language" first |

A gRPC "contract" is written in a `.proto` file, and both services share it:

```protobuf
service CustomerService {
  rpc GetCustomer (CustomerRequest) returns (CustomerReply);
}
message CustomerRequest { int32 id = 1; }
message CustomerReply   { int32 id = 1; string name = 2; }
```

**What to remember:** If you change a `.proto` file, **every** service that uses it must be updated. That is why "shared libraries" matter so much in this system.

---

### 3.4 Reverse Proxy (Nginx or Traefik)

**What is it?** A program that sits at the front door. Every request from the internet hits it first.

**Why is it here?**
1. **Routing:** it looks at the URL and decides where to send the request.
2. **HTTPS:** it holds the SSL certificates and handles encryption.
3. **Hiding the inside:** users never see the 27 internal services directly.

```
https://app.company.com/            → Angular customer portal
https://app.company.com/api/orders  → Orders service
https://admin.company.com/          → Node.js admin app
```

**What to remember:** If a certificate expires (the files are stored on the host, for example in `/home/...`), the **whole site goes down**. Someone or some script must renew them.

---

### 3.5 Docker and Containers

**What is it?** Docker packs a program **together with everything it needs** (the runtime, libraries, config) into an **image**. A running image is called a **container**.

**Why is it here?** "It works on my machine" stops being a problem. The exact same image runs on a laptop, in UAT, and in Production.

**Key words:**

| Word | Meaning | Analogy |
|---|---|---|
| **Dockerfile** | The recipe that describes how to build the image | A recipe card |
| **Image** | The built, frozen package | A sealed meal box |
| **Container** | A running copy of an image | The meal box opened and being eaten |
| **Tag** | A version label on an image, for example `orders:1.4.2` | The date stamp on the box |

**Multi-stage Dockerfile** (used in this system). The first stage builds the code with the heavy SDK. The second stage copies only the finished result into a small runtime image:

```dockerfile
# Stage 1: build (big image with the full .NET SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

# Stage 2: run (small image, runtime only)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Orders.Api.dll"]
```

---

### 3.6 Docker Swarm

**What is it?** Swarm joins several servers (called **nodes**) together and treats them as **one big computer** for running containers.

**Why is it here?** With 33 apps, you can't start containers by hand. Swarm:
- **places** containers on nodes that have free capacity
- **restarts** containers that crash
- **moves** containers to another node if a server dies
- **scales:** "run 3 copies of Orders" → Swarm makes it happen
- **does rolling updates:** replaces old containers with new ones one at a time, so there is no downtime

**Key words:**

| Word | Meaning |
|---|---|
| **Node** | One server in the Swarm. **Managers** make decisions, **workers** run containers |
| **Service** | "Run image X with N copies." Swarm keeps it that way |
| **Replica / Task** | One running copy of a service |
| **Stack** | A group of services defined together in one compose YAML file |
| **Overlay network** | A private virtual network that spans all nodes, so containers can find each other by name (for example `http://orders-api`) |
| **Secret / Config** | Passwords or config files that Swarm hands securely to containers |

**Example stack file (`docker-compose.yml`):**

```yaml
version: "3.8"
services:
  orders-api:
    image: myregistry.azurecr.io/orders-api:1.4.2
    deploy:
      replicas: 3
      update_config:
        parallelism: 1      # update one copy at a time
        order: start-first  # start the new one before stopping the old one
      rollback_config:
        parallelism: 1
    networks: [backend]
    secrets: [sql_connection]

networks:
  backend:
    driver: overlay

secrets:
  sql_connection:
    external: true
```

**What to remember:** Swarm's "desired state" idea. You tell Swarm *what you want* ("3 copies of version 1.4.2"), and Swarm keeps working until that is true.

---

### 3.7 Portainer

**What is it?** A web dashboard on top of Docker Swarm.

**Why is it here?** It lets people see and manage stacks, services, logs and containers by clicking instead of typing commands. It can also expose a **webhook** (a URL that, when called, makes it redeploy a service with the newest image). The pipeline uses this to trigger deployments.

**⚠️ The big risk ("Portainer drift"):** Portainer lets you **edit a stack's YAML directly in the browser**. If someone does that, the live config no longer matches what is in Git. Later, nobody knows what the "real" config is. This is one of the main things the takeover team wants to check.

**What to remember:** The truth should live in **Git**, not only in Portainer.

---

### 3.8 Azure DevOps (Repos and Pipelines)

**What is it?** Microsoft's platform for:
- **Repos:** stores the code of the 33 repositories (Git)
- **Pipelines:** automatic build and deploy steps (CI/CD)

**Key words:**

| Word | Meaning |
|---|---|
| **CI (Continuous Integration)** | Every code change is automatically built and tested |
| **CD (Continuous Deployment)** | Successful builds are automatically deployed |
| **Agent / Agent pool** | The machine that actually runs the pipeline steps. **Hosted** = Microsoft's machines. **Self-hosted** = your (or the vendor's) own machines |
| **PR policy** | Rules like "at least 1 reviewer must approve before merging" |
| **Branch / Tag** | `main`, `release/2.3`, `v2.3.1`: the labels that say which code is in UAT and which is in Prod |

**⚠️ Risk:** If the pipeline runs on the **vendor's self-hosted agents**, it will stop working when the vendor leaves.

➡️ **Section 4** walks through a complete pipeline file line by line.

---

### 3.9 Azure Container Registry (ACR)

**What is it?** A private storage place for Docker images, hosted in Azure.

**Why is it here?** The pipeline builds an image and **pushes** it to ACR. Swarm nodes **pull** images from ACR when they deploy.

```
myregistry.azurecr.io/orders-api:1.4.1   ← previous version (used for rollback)
myregistry.azurecr.io/orders-api:1.4.2   ← current version
```

**What to remember:** Old tags are your **rollback safety net**. If they're deleted, you can't go back.

---

### 3.10 SQL Server 2022 with Always On Availability Groups

**What is it?** Microsoft's relational database. Data is stored in tables with rows and columns.

**Why is it here?** It holds the important business data: customers, orders, invoices. There are **21 databases**.

**What is "Always On AG"?** A way of keeping **copies of a database on more than one server**:

```
          ┌──────────────┐   continuous copy    ┌───────────────┐
 App ───► │  PRIMARY     │ ───────────────────► │  SECONDARY    │
          │ (read/write) │                      │ (standby)     │
          └──────────────┘                      └───────────────┘
                  ▲
          If the primary dies, the secondary takes over automatically.
          Apps connect through a "listener" name, so they don't need to change.
```

**⚠️ Shared databases:** 27 services share 21 databases, so some databases are used by **more than one service**. If service A changes a table, service B might break. That is why the question **"who owns the schema?"** matters.

**Schema migrations** (changes to table structure) can be done in two ways:

| Good way (automated) | Risky way (manual) |
|---|---|
| Changes are written in code (for example EF Core migrations) and stored in Git, then applied automatically on deploy | A DBA opens SSMS and runs a SQL script by hand |
| You can always see the history | Nobody knows exactly what was run, or when |

**TDE (Transparent Data Encryption):** encrypts the database files on disk. The **encryption keys and certificates** must be backed up. If they're lost, **backups can't be restored**.

---

### 3.11 SQL Agent Jobs and Background Workers

**What is it?**
- **SQL Agent jobs:** tasks that SQL Server runs on a schedule (for example "every night at 2 AM, archive old records")
- **Background workers:** .NET services with no web page that run tasks in a loop (for example "every 5 minutes, send pending emails")

**Why is it here?** Some work has to happen automatically, without a user clicking anything: reports, clean-ups, month-end calculations.

**⚠️ Risk:** If jobs were **created by clicking in SSMS** instead of being scripted in Git, they are easy to forget during a migration.

---

### 3.12 MinIO (Object Storage)

**What is it?** A file storage server that speaks the **Amazon S3** protocol. Think of it as a self-hosted "Google Drive for programs".

**Why is it here?** Databases are bad at storing big files. So:
- the **file** (PDF, image, scan) goes into a MinIO **bucket**
- the **database** stores only a small record pointing to it

```
SQL table: Documents
┌────┬──────────┬───────────────────────────────┐
│ Id │ OrderId  │ FilePath                      │
├────┼──────────┼───────────────────────────────┤
│ 1  │ 123      │ invoices/2026/10/inv-123.pdf  │ ──► file lives in MinIO
└────┴──────────┴───────────────────────────────┘
```

**Key words:** **Bucket** = a top-level folder. **Object** = a file. **Key** = the file's path inside the bucket.

**⚠️ Risk:** The database and MinIO must stay **in sync**. If you restore the database from last week but not MinIO, records may point to files that don't exist (or the other way round).

---

### 3.13 Redis (Cache)

**What is it?** An extremely fast **in-memory** key-value store. Data lives in RAM, not on disk.

**Why is it here?** Some data is read **very often** but changes **rarely** (for example a product list or user permissions). Asking SQL Server every time is slow. So:

```
1. Service asks Redis: "Do you have products:list?"
2a. YES (cache hit)  → return it immediately (about 1 ms)
2b. NO  (cache miss) → ask SQL Server (about 50 ms) → save in Redis for 10 minutes → return it
```

**Other uses:** login sessions, rate limiting, short-lived locks.

**What to remember:** Redis is **not** the source of truth. If Redis is wiped, the app should still work, just slower for a moment.

---

### 3.14 MongoDB

**What is it?** A **document database**. Instead of tables, it stores JSON-like documents that don't all need the same shape.

**Why is it here?** The source document doesn't say exactly. Common uses are logs, audit trails, flexible settings, or data whose structure changes often.

```json
{
  "_id": "6531...",
  "event": "OrderCreated",
  "orderId": 123,
  "details": { "items": 3, "channel": "web" }
}
```

---

### 3.15 Secrets: Azure Key Vault vs. `.env` files

**What is a secret?** Anything that must not be public: database passwords, API keys, certificate passwords.

| Good way | Risky way |
|---|---|
| **Azure Key Vault** or **Swarm secrets**: stored encrypted and access-controlled | **`.env` files** or shell scripts sitting on the server (for example `/home/lmkr/`) |
| You know where every secret is | Secrets are scattered, often copied, and easy to leak or lose |

---

### 3.16 External Integrations

| Integration | What it does |
|---|---|
| **IDP (Identity Provider)** | Handles login. Users sign in there and get a **token** (usually a JWT) that the services check on every request |
| **Payment gateway** | Processes card or online payments |
| **SMTP** | Sends emails (notifications, password resets, invoices) |

---

## 4. The CI/CD Pipeline in Detail

A **pipeline** is a list of steps, written in a YAML file **stored in the repo** (usually `azure-pipelines.yml`), that Azure DevOps runs automatically whenever code changes.

### 4.1 The stages of one pipeline

```
┌──────────────────────── STAGE 1: BUILD (CI) ────────────────────────┐
│ checkout → restore → build → unit tests → docker build → docker push│
└──────────────────────────────────┬──────────────────────────────────┘
                                   │ only if every step passed
                                   ▼
┌──────────────────────── STAGE 2: DEPLOY UAT (CD) ───────────────────┐
│ (optional approval) → update the Swarm service to the new image tag │
│ → smoke test (call /health)                                         │
└──────────────────────────────────┬──────────────────────────────────┘
                                   │ a person approves in Azure DevOps
                                   ▼
┌──────────────────────── STAGE 3: DEPLOY PROD (CD) ──────────────────┐
│ same image (never rebuilt) → update Prod Swarm → smoke test         │
└─────────────────────────────────────────────────────────────────────┘
```

> **Golden rule:** build the image **once**, and promote the **same image tag** from UAT to Prod. If you rebuild for Prod, you're deploying something nobody tested.

### 4.2 A backend pipeline, line by line (`orders-api`)

```yaml
# azure-pipelines.yml — lives in the root of the orders-api repo

trigger:                      # WHEN does it run?
  branches:
    include: [ main, release/* ]   # on every push/merge to these branches

pr:                           # also run Stage 1 for every Pull Request
  branches:
    include: [ main ]         # (PR builds test the code but don't deploy)

variables:
  imageName: 'orders-api'
  registryConnection: 'acr-connection'   # a "service connection" = saved login to ACR
  registry: 'myregistry.azurecr.io'
  tag: '$(Build.BuildId)'                # unique number per run, e.g. 4812

stages:
# ─────────────────────────────── STAGE 1 ───────────────────────────────
- stage: Build
  jobs:
  - job: BuildAndPush
    pool:
      vmImage: 'ubuntu-latest'     # Microsoft-HOSTED agent
      # name: 'OnPrem-Pool'        # ← SELF-HOSTED agent (your/vendor's machine)
    steps:
    - task: UseDotNet@2
      inputs:
        packageType: sdk
        version: '10.0.x'

    - script: dotnet test tests/Orders.Tests -c Release
      displayName: 'Run unit tests'          # if a test fails, the pipeline STOPS here

    - task: Docker@2
      displayName: 'Build and push image to ACR'
      inputs:
        command: buildAndPush
        containerRegistry: $(registryConnection)
        repository: $(imageName)
        Dockerfile: src/Orders.Api/Dockerfile
        buildContext: .
        tags: |
          $(tag)

# ─────────────────────────────── STAGE 2 ───────────────────────────────
- stage: DeployUAT
  dependsOn: Build
  condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
  jobs:
  - deployment: DeployToSwarm
    environment: 'uat'             # "environments" can require approvals and keep deploy history
    pool:
      name: 'OnPrem-Pool'          # must be an agent that can reach the Swarm manager
    strategy:
      runOnce:
        deploy:
          steps:
          - script: |
              docker service update --with-registry-auth \
                --image $(registry)/$(imageName):$(tag) \
                uat_orders-api
            displayName: 'Rolling update in Swarm'

          - script: curl --fail https://uat.company.local/api/orders/health
            displayName: 'Smoke test'

# ─────────────────────────────── STAGE 3 ───────────────────────────────
- stage: DeployProd
  dependsOn: DeployUAT
  jobs:
  - deployment: DeployToSwarm
    environment: 'production'      # approval gate: a named person must click "Approve"
    pool: { name: 'OnPrem-Pool' }
    strategy:
      runOnce:
        deploy:
          steps:
          - script: |
              docker service update --with-registry-auth \
                --image $(registry)/$(imageName):$(tag) \
                prod_orders-api
```

### 4.3 Three ways a pipeline can tell Swarm to deploy

| Method | How it works | Pros / cons |
|---|---|---|
| **A. Portainer webhook** | Pipeline calls `curl -X POST https://portainer/api/webhooks/<id>?tag=4812`. Portainer pulls the image and updates the service | Easy. But the webhook URL **is** a secret, and the config may only exist in Portainer |
| **B. `docker service update` on an agent** | A self-hosted agent on (or connected to) a Swarm manager runs the CLI command (as in the example above) | Clear and scriptable. Needs an agent with Docker access |
| **C. `docker stack deploy -c stack.yml`** | Pipeline deploys the **whole stack file from Git** | Best for "Git is the truth": it **overwrites Portainer drift** |

The vendor system uses some mix of A and B (Portainer webhooks and pipeline scripts). Finding out exactly which is a goal of the discovery meeting.

### 4.4 A frontend pipeline (Angular)

Same idea, different build steps. The Angular app is compiled into static files, and an **Nginx** container serves them:

```yaml
steps:
- task: NodeTool@0
  inputs: { versionSpec: '24.x' }
- script: npm ci                           # install exact versions from package-lock.json
- script: npx ng build --configuration production
- task: Docker@2
  inputs:
    command: buildAndPush
    containerRegistry: $(registryConnection)
    repository: 'web-portal'
    Dockerfile: Dockerfile                 # FROM nginx:alpine + COPY dist/ ...
    tags: $(Build.BuildId)
```

### 4.5 Pipeline vocabulary

| Term | Meaning |
|---|---|
| **Trigger** | What starts the pipeline (push, PR, schedule, manual) |
| **Stage → Job → Step** | Stages contain jobs, jobs contain steps. Each job runs on one agent |
| **Task** | A ready-made step from Microsoft (for example `Docker@2`, `UseDotNet@2`) |
| **Service connection** | A saved, secured login to an outside system (ACR, Azure, SSH) |
| **Variable group** | Shared variables for many pipelines. It can be **linked to Key Vault**, so secrets never sit in YAML |
| **Environment** | A deploy target (uat, production) with **approvals**, **checks** and deployment history |
| **Artifact** | A file a pipeline produces (for example a build output) for later stages |
| **Build.BuildId** | Built-in variable, a unique run number. Good for image tags |

### 4.6 Image tagging strategy

| Tag style | Example | Verdict |
|---|---|---|
| `latest` | `orders-api:latest` | ❌ Never deploy this to UAT/Prod. You can't tell which code it is, and you can't roll back to "the previous latest" |
| Build number | `orders-api:4812` | ✅ Unique, links back to the pipeline run |
| Git commit | `orders-api:3f9c2ab` | ✅ Links straight to the exact code |
| Semantic version | `orders-api:1.4.2` | ✅ Human-friendly, good for releases |

### 4.7 Multi-repo changes: deploy order matters

When one feature touches **two repos** (for example adding `deliveryDate` to the Orders API and showing it in Angular):

```
1. Backend first:  orders-api 4813 adds the new field (old frontend just ignores it) ✅
2. Frontend next:  web-portal 977 displays the field                                ✅

Wrong order: the new frontend asks for a field the old API doesn't send → blank or broken screen ❌
```

Rule: **make backend changes backward-compatible**, so the old frontend and the new backend can both work during the gap.

### 4.8 What goes wrong in real life (the things to look for)

| Should be | Often is |
|---|---|
| Every deploy goes through the pipeline | Someone runs `docker push` from their laptop "just this once" |
| All config is in Git | Environment variables were added by hand in Portainer |
| Secrets come from Key Vault / variable groups | Passwords are pasted into the YAML or into `.env` files on the server |
| Hosted or company-owned agents | Pipelines only work on the **vendor's private VM** |
| Database changes ship with the code | A DBA runs scripts by hand after the deploy |

You'll practise spotting every one of these in section 10.

---

## 5. Putting It Together: Step-by-Step Flows

### Flow A: A user views their order

```
 1. User opens https://app.company.com/orders/123
 2. Reverse proxy receives it → sends the page request to the Angular app
 3. Angular app loads in the browser
 4. User isn't logged in → redirected to the IDP → logs in → gets a JWT token
 5. Angular calls:  GET /api/orders/123   (with the token in the header)
 6. Reverse proxy routes /api/orders/* → Orders service
 7. Orders service checks the token is valid
 8. Orders service checks Redis: "order:123"?  → miss
 9. Orders service reads the order from SQL Server
10. Orders service needs the customer name → calls the Customers service over gRPC
11. Orders saves the result in Redis for next time
12. Orders returns JSON → Angular shows the page
```

### Flow B: A user uploads an invoice PDF

```
1. Angular sends the file:  POST /api/documents  (multipart upload)
2. Documents service uploads the file to MinIO → bucket "invoices", key "2026/10/inv-123.pdf"
3. Documents service writes a row in SQL Server pointing to that key
4. Documents service sends an email through SMTP: "Your invoice was received"
```

### Flow C: A developer ships a change (CI/CD)

This is the most important flow for the takeover.

```
 Developer laptop
      │ 1. git push → opens a Pull Request
      ▼
 Azure DevOps Repos
      │ 2. Reviewer approves (PR policy) → merged into main
      ▼
 Azure DevOps Pipeline (runs on an agent)
      │ 3. Restore → build → run tests
      │ 4. docker build  (uses the multi-stage Dockerfile)
      │ 5. docker tag    orders-api:1.4.2
      │ 6. docker push   → ACR
      ▼
 Azure Container Registry
      │ 7. Pipeline calls the Portainer webhook (or runs a docker service update script)
      ▼
 Portainer / Docker Swarm
      │ 8. Swarm pulls orders-api:1.4.2 from ACR
      │ 9. Rolling update: start new copy → check it's healthy → stop old copy → repeat
      ▼
 Reverse proxy
      │ 10. Automatically sends traffic to the new, healthy containers
      ▼
 Users see the new version, with no downtime
```

If the change touches **two repos** (for example a backend API and its Angular frontend), both pipelines must run, and the **order matters**. Usually the backend goes first, so the new frontend doesn't call an API that doesn't exist yet.

### Flow D: Something broke, so roll back

```
1. New version 1.4.2 is causing errors
2. Option 1: in Portainer or the CLI → docker service rollback orders-api
             Swarm goes back to the previous image (1.4.1)
3. Option 2: redeploy the stack with the tag set back to 1.4.1
4. If a database migration also ran, it may ALSO need undoing
   → this is the hard part, and why automated migrations matter
```

### Flow E: Nightly scheduled job

```
02:00  SQL Agent job "Archive old orders" starts
02:05  Moves orders older than 2 years into the archive tables
02:06  On failure → sends an alert email to the DBA
```

---

## 6. The Environments

Code moves through several copies of the system before reaching real users:

```
  Dev  ──►  QA  ──►  UAT  ──►  Production
 (devs)  (testers) (business   (real users)
                    sign-off)
```

| Environment | Where it runs | Notes |
|---|---|---|
| Dev / QA | **Vendor's cloud subscription** | ⚠️ Could disappear when the vendor leaves |
| UAT / Prod | **On-premise** (your own servers) | Swarm nodes, SQL Always On, MinIO |

**Data masking:** non-production environments should use **fake or scrambled** copies of real data, so real customer data doesn't leak.

---

## 7. Cheat Sheet

| Component | One-line summary |
|---|---|
| Angular | The website the user sees |
| Node.js admin | Internal admin website |
| .NET services | The business logic, split into 27 small apps |
| REST | Frontend ↔ backend, JSON over HTTP |
| gRPC | Backend ↔ backend, fast binary calls |
| Reverse proxy | Front door: routing and HTTPS |
| Docker | Packs each app into a portable box |
| Swarm | Runs and heals the boxes across servers |
| Portainer | Web dashboard for Swarm |
| Azure DevOps | Stores code, builds and deploys it |
| ACR | Warehouse for built images |
| SQL Server AG | Main data, copied across servers for safety |
| SQL Agent | Scheduled database jobs |
| MinIO | File storage (S3-style) |
| Redis | Super-fast temporary cache |
| MongoDB | Flexible JSON-style storage |
| Key Vault | Safe for passwords and keys |
| IDP | Login and identity |

---


## 8. Lab Setup

### 8.1 Real system vs. your lab

The lab lives in the GitHub repo **vendor-takeover-lab** and runs in **GitHub Codespaces**: a free cloud machine (4 CPUs, 16 GB) with Docker inside. Nothing needs installing on your laptop except a browser.

| Real system | Lab | Same idea? |
|---|---|---|
| 27 .NET Core services | **4 services + 1 worker**: `orders-api`, `customers-api`, `billing-api`, `documents-api`, `notification-worker` | ✅ |
| gRPC between services, REST for frontends | `orders-api` → `customers-api` and `billing-api` over **gRPC**. Portal → all APIs over **REST** | ✅ |
| 5 Angular apps | 1 Angular app: `web-portal` (served by Nginx) | ✅ |
| 1 Node.js admin | `admin-node`: health dashboard for every service | ✅ |
| 21 databases, some shared | 3 databases. **SalesDb is shared** by `orders-api` (schema `orders`), `billing-api` (schema `billing`) and `notification-worker` (plain SQL) | ✅ |
| SQL Server 2022 (Always On) + SQL Agent | **One lightweight SQL Server 2022 instance** with SQL Agent, its memory capped to about 1 GB. Always On is left out on purpose | ✅ Except Always On |
| MinIO, Redis, MongoDB | One container each, used by real code | ✅ |
| SMTP | **Mailpit**: catches every email in a web inbox | ✅ |
| Payment gateway | Fake "always approve" endpoint in `billing-api` | ✅ The idea |
| IDP | Keycloak, optional (`infra/keycloak.yml`); the services don't require login | ⚠️ Optional |
| Docker Swarm (multi-node) | Swarm in the Codespace. Extra nodes can be simulated with `scripts/add-workers.sh` (experimental) | ✅ |
| Portainer | Portainer CE | ✅ |
| Nginx / Traefik | **Traefik** routes by path, using labels in the stack file | ✅ |
| ACR | Local registry on port 5000 | ✅ Same push/pull/tag |
| Azure DevOps pipelines | Real pipeline files in `pipelines/`, run by a **self-hosted agent inside your Codespace**. Or `scripts/ci.sh`, which does the same steps | ✅ |
| Key Vault | Swarm secrets (exercise E11) | ✅ The idea |

### 8.2 How the lab is wired

```
Browser ──► :8000 Traefik ─┬─ /               → web-portal (Angular in Nginx)
                           ├─ /admin          → admin-node (Node.js)
                           ├─ /api/orders     → orders-api ──gRPC──► customers-api ──► SQL CustomersDb
                           │                       │  └────gRPC──► billing-api ──┐
                           │                       ├──► Redis (cache)            ▼
                           │                       └──► SQL SalesDb ◄── schemas orders + billing
                           ├─ /api/customers  → customers-api                    ▲
                           ├─ /api/billing    → billing-api                      │ plain SQL
                           └─ /api/documents  → documents-api ──► MinIO + SQL DocumentsDb
                                                                                 │
                                    notification-worker (every 30 s) ────────────┘
                                         ├──► Mailpit (SMTP)   └──► MongoDB (audit log)

Build & deploy:  scripts/ci.sh  or  Azure DevOps pipeline (agent in the Codespace)
                 → docker build → push localhost:5000 → docker service update → smoke test
```

### 8.3 Start the lab (one time)

1. Open **https://github.com/Abdulrehman-Malik/vendor-takeover-lab**.
2. Click **Code → Codespaces → ⋯ → New with options**, choose **4-core**, and click **Create codespace**.
3. Wait for the browser VS Code to open. `scripts/bootstrap.sh` runs by itself: it creates the Swarm, the `labnet` network, `infra/.env` with random passwords, the image registry and Portainer.
4. Open **Portainer** straight away (the **PORTS** tab → port 9443 → globe icon) and create the admin user. You have 5 minutes.

Everything else is in section 10, starting with E1.

> **Save your free hours.** The free plan gives about 30 hours a month on a 4-core machine. When you finish a session, stop the Codespace: **Code → Codespaces → ⋯ → Stop codespace** (it also stops by itself after 30 minutes idle). Your files, images and data are kept.

### 8.4 Opening the lab's web pages

In a Codespace, ports are reached through the **PORTS** tab, not `localhost` in your own browser. Click the globe icon next to a port to open it. Inside the Codespace terminal, `localhost` works normally (for example `curl localhost:8000/api/orders`).

| Port | What |
|---|---|
| 8000 | **Lab portal** (Traefik). `/admin` is the health dashboard |
| 8088 | Traefik dashboard: every route and service |
| 9443 | **Portainer** |
| 9001 | MinIO console (user `labadmin`, password `MINIO_ROOT_PASSWORD` in `infra/.env`) |
| 8025 | Mailpit inbox |
| 5000 | Image registry (`curl localhost:5000/v2/_catalog`) |
| 1433 | SQL Server (user `sa`, password `MSSQL_SA_PASSWORD` in `infra/.env`) |
| 8180 | Keycloak, if you deploy it |

To see the generated passwords: `cat infra/.env`.

### 8.5 Repo layout

```
vendor-takeover-lab/
├── 01-How-The-Application-Works.md   ← this guide
├── README.md                         ← quick start and Azure DevOps agent setup
├── .devcontainer/                    ← Codespaces machine: Docker, .NET 10, Node 24
├── src/
│   ├── shared-protos/                customers.proto, billing.proto (gRPC contracts)
│   ├── orders-api/                   REST + gRPC client + SQL + Redis   (Data/Migrations = EF Core)
│   ├── customers-api/                gRPC server (:8081) + REST (:8080) + CustomersDb
│   ├── billing-api/                  gRPC server + fake payment endpoint + SalesDb.billing
│   ├── documents-api/                file upload: MinIO + DocumentsDb
│   ├── notification-worker/          background worker: SQL → SMTP → MongoDB
│   ├── web-portal/                   Angular app + Nginx Dockerfile
│   └── admin-node/                   Node.js health dashboard
├── tests/Orders.Tests/               unit tests (the pipeline's quality gate)
├── db/
│   ├── agent-jobs/                   SQL Agent jobs as scripts (the good way)
│   └── manual-scripts/               hand-run "hotfix" SQL (the risky way)
├── infra/
│   ├── stack.yml                     the whole system as one Swarm stack
│   ├── versions.env                  image tag per service
│   ├── registry.yml, portainer.yml, keycloak.yml
│   └── .env.example                  shape of the generated secrets file
├── pipelines/                        Azure DevOps pipelines (+ templates/)
├── scripts/                          bootstrap, build, deploy, ci, sql, status, reset ...
└── docs/instructor-notes.md          answers for the audit exercise (spoilers!)
```

### 8.6 The scripts

Run them from the repo root in the Codespace terminal.

| Command | What it does |
|---|---|
| `bash scripts/bootstrap.sh` | Swarm, network, secrets, registry, Portainer (runs on every Codespace start) |
| `bash scripts/build.sh all` | Build and push every image with the tags in `infra/versions.env` |
| `bash scripts/build.sh orders-api 7` | Build and push one image with a chosen tag |
| `bash scripts/deploy.sh` | `docker stack deploy` the whole system from Git, then caps SQL Server's memory |
| `bash scripts/sql-tune.sh [MB]` | Cap SQL Server's cache (default 768 MB) so it uses about 1 GB |
| `bash scripts/ci.sh orders-api` | The pipeline, locally: test → build → push → rolling update → smoke test |
| `bash scripts/deploy-service.sh orders-api 7` | Rolling update of one service to tag 7 |
| `bash scripts/status.sh` | Nodes, stacks, services, and HTTP checks through Traefik |
| `bash scripts/sql.sh -Q "..."` | Run SQL in the SQL Server container (`-d SalesDb`, `-i file.sql`) |
| `bash scripts/add-workers.sh 2` | Experimental: add 2 simulated worker nodes (`remove` to undo) |
| `bash scripts/plant-deviations.sh` | Acts as the vendor: makes changes outside Git (exercise E15) |
| `bash scripts/reset.sh [--data]` | Remove the stack (and, with `--data`, all databases and files) |

### 8.7 Running on Docker Desktop instead (optional)

The same repo works on a laptop with Docker Desktop, run from a **WSL Ubuntu** terminal (the scripts are bash). The full stack uses about 3–4 GB inside Docker (SQL Server about 1 GB of that, thanks to `scripts/sql-tune.sh`). Your laptop has 8 GB in total, so close other heavy apps first, or use Codespaces.

---

## 9. Try Each Component on Its Own

Short warm-ups with plain Docker commands, in the Codespace terminal. Do them **before** E1 (or after `bash scripts/reset.sh`), because some use the same ports as the lab stack. `bootstrap.sh` has already set up the Swarm, the `labnet` network, the registry and Portainer.

### 9.1 Docker: images and containers

```bash
docker run --rm hello-world
```

```bash
docker run -d --name web1 -p 8080:80 nginx:alpine
```

```bash
curl -s localhost:8080 | head -n 5
```

```bash
docker ps
```

```bash
docker logs web1
```

```bash
docker exec -it web1 sh
```

Inside the container, run `ls /usr/share/nginx/html`, then `exit`.

```bash
docker rm -f web1
```

✅ **You learned:** an image is a package, a container is a running copy, `-p host:container` opens a port.

### 9.2 Swarm: services, scaling, self-healing, rollback

```bash
docker node ls
```

```bash
docker service create --name web --replicas 3 -p 8081:80 nginx:alpine
```

```bash
docker service ps web
```

**Self-healing:** kill one container and watch Swarm replace it.

```bash
docker rm -f $(docker ps -q -f name=web. | head -n 1)
```

```bash
docker service ps web
```

One task is now **Failed/Shutdown** and a new one is **Running**.

**Scale, update, roll back:**

```bash
docker service scale web=5
```

```bash
docker service update --image nginx:1.27-alpine --update-parallelism 1 --update-delay 5s web
```

```bash
docker service rollback web
```

```bash
docker service rm web
```

✅ **You learned:** desired state, replicas, self-healing, rolling update and rollback.

### 9.3 Overlay network and service discovery

```bash
docker service create --name hello --network labnet traefik/whoami
```

```bash
docker run --rm --network labnet curlimages/curl -s http://hello
```

The second container found the first **by name**. This is how `orders-api` reaches `http://customers-api:8081`.

```bash
docker service rm hello
```

### 9.4 Image registry (stand-in for ACR)

`bootstrap.sh` already runs the registry on port 5000. Tag and push an image, like a pipeline does:

```bash
docker pull nginx:alpine
```

```bash
docker tag nginx:alpine localhost:5000/my-nginx:1.0
```

```bash
docker push localhost:5000/my-nginx:1.0
```

```bash
curl -s localhost:5000/v2/_catalog
```

```bash
curl -s localhost:5000/v2/my-nginx/tags/list
```

✅ **You learned:** tag → push → registry → Swarm pulls. The ACR flow.

### 9.5 Portainer

Open port **9443** from the PORTS tab and log in. Explore **Stacks** (you'll see `registry` and `portainer`), **Services**, **Networks**, **Secrets**, and a service's **Logs**. On a service page, find the **Service webhook** toggle: that's how a pipeline can trigger a redeploy.

✅ **You learned:** Portainer is a UI over the same Swarm you drive with commands.

### 9.6 Traefik routing

The lab's Traefik lives in `infra/stack.yml`. Here's a standalone mini version. Save it as `/tmp/traefik-demo.yml`:

```yaml
services:
  traefik:
    image: traefik:v3.5
    command:
      - --providers.swarm=true
      - --providers.swarm.exposedByDefault=false
      - --providers.swarm.network=labnet
      - --entrypoints.web.address=:80
      - --api.insecure=true
    ports: ["8000:80", "8088:8080"]
    volumes: ["/var/run/docker.sock:/var/run/docker.sock:ro"]
    networks: [labnet]
  whoami:
    image: traefik/whoami
    networks: [labnet]
    deploy:
      replicas: 2
      labels:
        - traefik.enable=true
        - traefik.http.routers.whoami.rule=PathPrefix(`/whoami`)
        - traefik.http.services.whoami.loadbalancer.server.port=80
networks:
  labnet:
    external: true
```

```bash
docker stack deploy -c /tmp/traefik-demo.yml proxy
```

Run this a few times and watch `Hostname` alternate between the 2 replicas:

```bash
curl -s localhost:8000/whoami | grep Hostname
```

```bash
docker stack rm proxy
```

✅ **You learned:** routes are labels on each service. Compare with the `traefik.*` labels in `infra/stack.yml`.

### 9.7 SQL Server and SQL Agent

```bash
export SA_PASSWORD="$(grep ^MSSQL_SA_PASSWORD= infra/.env | cut -d= -f2-)"
```

```bash
docker run -d --name sql -p 1433:1433 -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD="$SA_PASSWORD" -e MSSQL_AGENT_ENABLED=true mcr.microsoft.com/mssql/server:2022-latest
```

Wait about 30 seconds, then:

```bash
docker exec sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$SA_PASSWORD" -Q "CREATE DATABASE Demo"
```

```bash
docker exec sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$SA_PASSWORD" -d Demo -Q "CREATE TABLE T (Id INT IDENTITY PRIMARY KEY, Name NVARCHAR(50)); INSERT INTO T (Name) VALUES ('Ali'), ('Sara'); SELECT * FROM T;"
```

You can also browse it with the **SQL Server** extension in the Codespace's left sidebar: server `localhost,1433`, user `sa`, tick *Trust server certificate*.

```bash
docker rm -f sql
```

Once the lab runs, `bash scripts/sql.sh` does the same against the lab's SQL Server, and `db/agent-jobs/ArchiveOldOrders.sql` shows a SQL Agent job written as a script (exercise E12).

> **Why so much memory?** SQL Server refuses to start with less than 2 GB available, then fills most of it with cache. The lab caps the cache with `max server memory` (`scripts/sql-tune.sh`), so it really uses about 1 GB. Run `docker stats --no-stream` before and after to see the difference.
>
> **About Always On:** the lab uses one instance, with no Availability Group. Section 3.10 explains how AGs work in the real system.

### 9.8 MinIO

```bash
docker run -d --name minio-demo -p 9000:9000 -p 9001:9001 -e MINIO_ROOT_USER=demo -e MINIO_ROOT_PASSWORD=demo-password-123 minio/minio server /data --console-address ":9001"
```

Open port **9001**, log in with `demo` / `demo-password-123`, create a bucket `invoices`, and upload any file into a folder `2026/10/`.

```bash
docker rm -f minio-demo
```

> In 2025 MinIO cut its community console back to a basic object browser and moved community releases to source only. The existing `minio/minio` image is fine for learning. In the real system, ask which version and licence they run.

✅ **You learned:** buckets, objects and keys. A database row only stores the **key**.

### 9.9 Redis

```bash
docker run -d --name redis-demo redis:7-alpine
```

```bash
docker exec -it redis-demo redis-cli
```

Inside `redis-cli`, act out the cache pattern:

```
GET order:123                    → (nil)  = cache MISS, go to SQL
SET order:123 "{\"id\":123}" EX 30
GET order:123                    → value  = cache HIT
TTL order:123                    → seconds left
FLUSHALL                         → wipe everything; the app must still work
exit
```

```bash
docker rm -f redis-demo
```

### 9.10 MongoDB

```bash
docker run -d --name mongo-demo mongo:7
```

```bash
docker exec -it mongo-demo mongosh
```

```javascript
use labdb
db.audit.insertOne({ event: "OrderCreated", orderId: 123, details: { items: 3 } })
db.audit.insertOne({ event: "UserLogin", user: "ali" })   // a different shape is fine
db.audit.find()
exit
```

```bash
docker rm -f mongo-demo
```

### 9.11 Secrets: environment variables vs. Swarm secrets

The risky way:

```bash
docker service create --name env-demo -e DB_PASSWORD=SuperSecret123 alpine sleep 3600
```

```bash
docker service inspect env-demo --format '{{json .Spec.TaskTemplate.ContainerSpec.Env}}'
```

The password is printed in plain text. Anyone with Docker or Portainer access can read it.

The better way:

```bash
printf 'SuperSecret123' | docker secret create db_password -
```

```bash
docker service create --name secret-demo --secret db_password alpine sh -c "cat /run/secrets/db_password; sleep 3600"
```

```bash
docker secret inspect db_password
```

```bash
docker service logs secret-demo
```

`docker secret inspect` doesn't show the value. It only exists inside the container, as a file in `/run/secrets/` (the demo prints it to its log on purpose).

```bash
docker service rm env-demo secret-demo && docker secret rm db_password
```

### 9.12 SMTP with Mailpit

```bash
docker run -d --name mailpit-demo -p 8025:8025 -p 1025:1025 axllent/mailpit
```

```bash
curl -s --url smtp://localhost:1025 --mail-from app@lab.local --mail-rcpt user@lab.local --upload-file - <<'EOF'
Subject: Your invoice

Invoice 123 received.
EOF
```

Open port **8025** to see the email. Nothing is really sent.

```bash
docker rm -f mailpit-demo
```

---

## 10. End-to-End Exercises

Run everything from the repo root in the Codespace terminal.

### Suggested path

| Week | Exercises | Theme |
|---|---|---|
| 1 | Section 9, then E1–E3 | Components, first deploy, first change |
| 2 | E4–E8 | Contracts, rollback, scaling, cache, files |
| 3 | E9–E12 | Database changes, drift, secrets, jobs |
| 4 | E13–E15 | Real pipeline, restart drill, takeover audit |

---

### E1. First full deploy

**Goal:** get the whole system running from Git.

```bash
bash scripts/build.sh all
```

The first build takes about 10 minutes (it downloads the .NET, Node and SQL Server images).

```bash
bash scripts/deploy.sh
```

```bash
watch docker stack services lab
```

Wait until every line shows `1/1` (or `2/2` for orders-api), then press `Ctrl+C`. SQL Server is slowest. The services retry their database migrations until it's ready.

**Check:**
1. `bash scripts/status.sh` shows `200` for every path.
2. Open port **8000**: create an order, then click **Details**.
3. Open **8000/admin**: everything is healthy.
4. After about 30 seconds, the invoice shows **Email sent: yes**, and the email is in **Mailpit** (port 8025).

**Teaches:** one stack file describes the whole system. One order touched 7 components: Traefik, Angular, orders-api, customers-api (gRPC), billing-api (gRPC), SQL Server, and later the worker, Mailpit and MongoDB.

---

### E2. Change code and ship it

**Goal:** follow one change from code to running container.

1. In `src/orders-api/Program.cs`, change the version endpoint to:
   ```csharp
   api.MapGet("/version", () => new { service = "orders-api", version, message = "Hello from my first change" });
   ```
2. In a **second terminal**, watch the tasks:
   ```bash
   watch docker service ps lab_orders-api
   ```
3. In the first terminal, run the local pipeline:
   ```bash
   bash scripts/ci.sh orders-api
   ```

**Check:** you see the 2 replicas replaced **one at a time** (new one starts, old one stops), the smoke test prints your message, and the portal badge shows the new version. `git diff infra/versions.env` shows the new tag.

**Teaches:** Flow C from section 5. Commit `infra/versions.env` so Git matches what's running.

---

### E3. A change across backend and frontend

**Goal:** see why deploy order matters.

1. Backend: in `src/orders-api/Program.cs`, add a `DateTime DeliveryDate` parameter at the end of the `OrderDetails` record, and pass `DeliveryDate: order.CreatedAt.AddDays(3)` where it's created.
2. Frontend: add `deliveryDate: string;` to `OrderDetails` in `src/web-portal/src/app/api.ts`, and show `{{ d.deliveryDate | date }}` in the details box in `app.html`.
3. Deploy the **frontend first**: `bash scripts/ci.sh web-portal`. Open an order's details. What's shown where the date should be?
4. Now deploy the backend: `bash scripts/ci.sh orders-api`. Refresh.

**Teaches:** section 4.7. Backward-compatible backend changes go first.

---

### E4. Change a gRPC contract

**Goal:** understand shared contracts.

1. List and call the gRPC service directly:
   ```bash
   docker run --rm --network labnet fullstorydev/grpcurl -plaintext customers-api:8081 list
   ```
   ```bash
   docker run --rm --network labnet fullstorydev/grpcurl -plaintext -d '{"id": 1}' customers-api:8081 customers.CustomerService/GetCustomer
   ```
2. **Safe change:** in `src/shared-protos/customers.proto`, add `string phone = 4;` to `CustomerReply`. Deploy **only** customers-api (`bash scripts/ci.sh customers-api`). Open an order's details: still works.
3. **Breaking change:** change `string name = 2;` to `int32 name = 2;`, set `Name = customer.Id` in `CustomerGrpcService.cs` so it compiles, and deploy only customers-api. Open an order's details again.
4. Undo the breaking change and redeploy.

**Check:** step 2 keeps working; step 3 breaks orders-api's calls.

**Teaches:** adding fields is safe; changing a field's type or number breaks every service sharing the contract.

---

### E5. Rollback drill

**Goal:** the drill the vendor will be asked to demonstrate.

**Part A: automatic rollback.** Release a "broken" version whose health check fails:

```bash
docker service update --env-add FORCE_UNHEALTHY=true lab_orders-api
```

Watch it: the new task never becomes healthy, so Swarm **rolls back by itself** (`failure_action: rollback` in `stack.yml`):

```bash
docker service ps lab_orders-api
```

```bash
docker service inspect lab_orders-api --format '{{.UpdateStatus.State}}: {{.UpdateStatus.Message}}'
```

**Part B: manual rollback.** A release can be healthy but still wrong:

1. Change `OrderPricing.BulkDiscount` to `0.50m` (50% off!). The unit tests now fail, which is the quality gate doing its job. To get the "bad" release out anyway (as a careless team might), update the expected values in `tests/Orders.Tests/OrderPricingTests.cs` to `25.00m` and `16.67m`.
2. `bash scripts/ci.sh orders-api`. Create an order with 10 items: wrong price.
3. Roll back by hand and time it:
   ```bash
   time docker service rollback lab_orders-api
   ```
4. Revert your code change.

**Part C: billing-api.** Look at `billing-api` in `stack.yml`. What would happen if *it* had a broken release?

**Teaches:** automatic vs. manual rollback, why old image tags must exist, and that a missing healthcheck means no automatic rollback.

---

### E6. Scale and self-heal under load

```bash
docker service scale lab_orders-api=3
```

```bash
for i in $(seq 1 6); do curl -s localhost:8000/api/orders/whoami; echo; done
```

Different container names answer. Now, in a second terminal, keep calling the API:

```bash
while true; do curl -s -o /dev/null -w '%{http_code} ' localhost:8000/api/orders/whoami; sleep 0.3; done
```

In the first terminal, kill one replica:

```bash
docker rm -f $(docker ps -q -f name=lab_orders-api | head -n 1)
```

**Check:** the loop keeps printing (mostly) `200`, and `docker service ps lab_orders-api` shows a replacement. Scale back with `docker service scale lab_orders-api=2`.

**Teaches:** Traefik load-balances across replicas; Swarm heals.

---

### E7. Watch the cache work

1. Open an order's details in the portal twice within 60 seconds.
2. ```bash
   docker service logs lab_orders-api 2>&1 | grep -E "Cache (HIT|MISS)" | tail
   ```
   (With 2 replicas, each one has to warm up separately, but they share one Redis.)
3. Check the key in Redis:
   ```bash
   docker exec -it $(docker ps -q -f name=lab_redis) redis-cli KEYS 'order:*'
   ```
4. **Stale data:** change the order in SQL, then open details again within 60 s: you still see the old value.
   ```bash
   bash scripts/sql.sh -d SalesDb -Q "UPDATE orders.Orders SET Product = 'Changed in SQL' WHERE Id = 1"
   ```
5. **Redis down:** `docker service scale lab_redis=0`. The portal still works, and the logs say "Redis unavailable". Then `docker service scale lab_redis=1`.

**Teaches:** caching speed-ups, stale data, and why Redis is never the source of truth.

---

### E8. Files in MinIO, records in SQL

1. Upload a PDF in the portal's **Documents** section.
2. Find the file in the MinIO console (port 9001, bucket `documents`) and its row:
   ```bash
   bash scripts/sql.sh -d DocumentsDb -Q "SELECT Id, ObjectKey FROM Documents"
   ```
3. Delete the file **only in MinIO** (console → select → Delete), then click **Download** in the portal.

**Check:** you get "SQL Server has a record ... but the file is missing from MinIO".

**Teaches:** the database and the file store must be backed up and restored **together**.

---

### E9. Database changes: migrations vs. manual scripts

1. **Automated:** add `public string? Notes { get; set; }` to the `Order` class in `src/orders-api/Data/OrdersDbContext.cs`, then:
   ```bash
   dotnet tool restore
   ```
   ```bash
   dotnet ef migrations add AddNotes --project src/orders-api --output-dir Data/Migrations
   ```
   ```bash
   bash scripts/ci.sh orders-api
   ```
   The service applies the migration on start-up. Check the history table:
   ```bash
   bash scripts/sql.sh -d SalesDb -Q "SELECT MigrationId FROM orders.__EFMigrationsHistory"
   ```
2. **Manual:** apply the DBA's hotfix by hand:
   ```bash
   bash scripts/sql.sh -i db/manual-scripts/2026-09-14_hotfix_add_priority.sql
   ```
3. **Compare** the real table with what Git knows:
   ```bash
   bash scripts/sql.sh -d SalesDb -Q "SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('orders.Orders')"
   ```
   `Notes` is in a migration; `Priority` is not.
4. Rebuild from scratch: `bash scripts/reset.sh --data`, `bash scripts/deploy.sh`, then run step 3 again. `Priority` is gone.

**Teaches:** manual scripts create schema drift that a rebuild silently loses. This is why the takeover plan includes a schema comparison.

---

### E10. Create and catch drift

1. In **Portainer** → Services → `lab_orders-api`: change replicas to 4 and add the environment variable `FEATURE_X=true`. Apply.
2. Compare the live service with Git:
   ```bash
   docker service inspect lab_orders-api --format 'replicas={{.Spec.Mode.Replicated.Replicas}} image={{.Spec.TaskTemplate.ContainerSpec.Image}}'
   ```
   ```bash
   docker service inspect lab_orders-api --format '{{json .Spec.TaskTemplate.ContainerSpec.Env}}' | tr ',' '\n' | grep -v -i -E 'password|secret'
   ```
   versus the `orders-api` block in `infra/stack.yml`.
3. Write down every difference.
4. Put it right: decide what's correct, put it **in Git**, then `bash scripts/deploy.sh`.

**Check:** after step 4, the live service matches the file. (Anything you didn't add to Git is gone.)

**Teaches:** deviation zone #1 and how to detect it.

---

### E11. Move a connection string into a Swarm secret

1. See the problem: the SA password is in plain text in the service definition.
   ```bash
   docker service inspect lab_orders-api --format '{{json .Spec.TaskTemplate.ContainerSpec.Env}}' | tr ',' '\n' | grep Sales
   ```
2. Create a secret from the current value:
   ```bash
   grep '^ConnectionStrings__Sales=' infra/.env | cut -d= -f2- | tr -d '\n' | docker secret create ConnectionStrings__Sales -
   ```
3. In `infra/stack.yml`, add to `orders-api`, `billing-api` and `notification-worker`:
   ```yaml
       secrets: [ConnectionStrings__Sales]
   ```
   and at the bottom of the file:
   ```yaml
   secrets:
     ConnectionStrings__Sales:
       external: true
   ```
4. Delete the `ConnectionStrings__Sales=` line from `infra/.env`, then `bash scripts/deploy.sh`.
5. Run step 1 again, and place an order to prove the services still reach SQL Server.

**Check:** the connection string is gone from the service definition. The apps read `/run/secrets/ConnectionStrings__Sales` (see `AddKeyPerFile` in each `Program.cs`).

**Teaches:** deviation zone #5. Where secrets should and shouldn't live.

---

### E12. SQL Agent jobs as code

```bash
bash scripts/sql.sh -i db/agent-jobs/ArchiveOldOrders.sql
```

After 5 minutes:

```bash
bash scripts/sql.sh -Q "EXEC msdb.dbo.sp_help_jobhistory @job_name = N'ArchiveOldOrders'"
```

Now delete the job (`bash scripts/sql.sh -Q "EXEC msdb.dbo.sp_delete_job @job_name = N'ArchiveOldOrders'"`) and recreate it from the script.

**Teaches:** a job that only exists on the server is lost in a migration. A script can always be replayed.

---

### E13. A real Azure DevOps pipeline with a quality gate

1. Set up Azure DevOps and the agent in your Codespace: see **README.md → Azure DevOps pipelines**.
2. Create a pipeline from `pipelines/orders-api.yml`. Push a change to `src/orders-api`: watch Build → Deploy.
3. Push a change that **breaks a unit test**: the run stops at the test step and nothing is deployed.
4. In Azure DevOps → Environments → **lab** → Approvals and checks, add yourself as an approver. Push again: Deploy waits for your click.
5. After a pipeline deploy, run `bash scripts/deploy.sh`. What happens to the version the pipeline deployed, and why? (Hint: compare `infra/versions.env` with `docker service inspect`.)

**Teaches:** section 4 end to end, plus a classic drift source: pipelines that don't write back to Git.

---

### E14. What happens on a restart?

1. With the full lab running, stop the Codespace (**Code → Codespaces → ⋯ → Stop codespace**) and start it again.
2. Run `bash scripts/status.sh` and `docker ps -a`.

**Check:** the Swarm services come back by themselves; the data in SQL Server, MongoDB and MinIO is still there (named volumes); Redis starts empty (it's a cache). Containers started with plain `docker run` in section 9 stay stopped.

**Teaches:** the discovery question *"If a Swarm node reboots, does everything recover automatically?"*

**Bonus (experimental):** `bash scripts/add-workers.sh 2`, then `docker service update --force lab_orders-api` and `docker service ps lab_orders-api` to see tasks on other nodes. Drain a node with `docker node update --availability drain lab-worker-1` and watch the tasks move.

---

### E15. Capstone: audit the "vendor's" system

1. Make sure the full stack is healthy, then let the "vendor" make their changes:
   ```bash
   bash scripts/plant-deviations.sh
   ```
2. Don't read the script. Fill in this table using only commands, Portainer and the repo:

| Question | Your answer | Evidence (command / screen) |
|---|---|---|
| Does every live service match `infra/stack.yml` and `infra/versions.env`? | | |
| Is every image running one that a pipeline built? | | |
| Does every service have a pipeline? | | |
| Is every database column in a migration? | | |
| Is every SQL Agent job scripted in Git? | | |
| Where does every secret live? Any copies on the host? | | |
| Which services can roll back automatically? | | |
| Who else reads or writes the shared SalesDb? | | |
| What comes back after a restart? | | |

3. Check yourself against `docs/instructor-notes.md`.

**Teaches:** this is the checklist for the vendor discovery meeting. After the lab, you'll recognise each risk when you see it live.

---

### Troubleshooting

| Problem | Command |
|---|---|
| A service stays at `0/1` | `docker service ps lab_<name> --no-trunc` (full error) |
| See a service's output | `docker service logs -f lab_<name>` |
| What config is it really running? | `docker service inspect lab_<name> --pretty` |
| Who uses how much memory? | `docker stats --no-stream` |
| Is a route set up? | Traefik dashboard, port 8088 |
| Can service A reach B? | `docker run --rm --network labnet curlimages/curl -v http://<service>:<port>/health` |
| Portainer says "timed out" | `docker service update --force portainer_portainer`, then create the admin user within 5 minutes |
| Start over completely | `bash scripts/reset.sh --data`, then `bash scripts/deploy.sh` |
