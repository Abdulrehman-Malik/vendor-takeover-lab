# The Lab in Plain English

> **Who is this for?** Anyone who is completely new: no programming or IT background needed.
> Everything is explained in **simple words**, with **pictures** and **everyday examples**.
>
> **Tip:** In VS Code, open this file and press **Ctrl + Shift + V** to see it nicely formatted.
>
> Prefer Urdu? Read [Asaan-Urdu-Guide.md](Asaan-Urdu-Guide.md).

---

## 1. What is this lab?

A company (the "vendor") built a large software system. Your team is going to **take it over** and run it themselves.

The real system is big (33 projects, 21 databases). So we built a **small working model** of it, to learn safely:

- what the parts of the system are,
- how they talk to each other,
- how code becomes a running service,
- and where the vendor may have left hidden problems.

**Example:** A pilot practises in a **flight simulator** before flying a real plane. This lab is your simulator.

---

## 1A. The whole system in one picture 🖼️

Just look at this picture first. Everything else can wait.

```
                         🧑 YOU (web browser)
                               │
                               │  "I want 10 Widgets"
                               ▼
                    ┌─────────────────────┐
                    │  🚪 FRONT DOOR       │   Traefik
                    │  "Where would you    │   (everything comes in through here)
                    │   like to go?"       │
                    └──────────┬──────────┘
                               │
          ┌────────────────────┼────────────────────┐
          ▼                    ▼                    ▼
   ┌─────────────┐     ┌──────────────┐     ┌──────────────┐
   │ 🖥️ WEBSITE   │     │ 🧑‍🍳 WAITER    │     │ 📁 FILES      │
   │ web-portal  │     │ orders-api   │     │ documents-api│
   │ (what you   │     │ (takes the   │     │ (keeps the   │
   │  see)       │     │  order)      │     │  paperwork)  │
   └─────────────┘     └──────┬───────┘     └──────┬───────┘
                              │                    │
                 ┌────────────┴─────────┐          │
                 ▼                      ▼          ▼
         ┌──────────────┐       ┌──────────────┐  ┌──────────────┐
         │ 📒 GUEST BOOK │       │ 💰 CASHIER    │  │ 📦 STOREROOM  │
         │ customers-api│       │ billing-api  │  │ MinIO        │
         │ "Who is this │       │ "Here is the │  │ (files are   │
         │  customer?"  │       │  bill"       │  │  kept here)  │
         └──────────────┘       └──────┬───────┘  └──────────────┘
                                       │
                                       ▼
                               ┌──────────────┐        ┌──────────────┐
                               │ 🏃 MESSENGER  │ ─────► │ 📮 POST OFFICE│
                               │ notification │  email │ Mailpit      │
                               │ -worker      │        └──────────────┘
                               └──────────────┘

   Everyone's data:  📚 SQL Server (main records book)   ⚡ Redis (quick memory)   📓 MongoDB (diary)
```

### What each part is for, in one line

| Icon | Name | Its job in one line | What if it's missing? |
|---|---|---|---|
| 🚪 | Traefik | Sends every visitor to the right counter | Nobody can reach any service |
| 🖥️ | web-portal | Shows you the buttons and forms | You can't place an order |
| 🧑‍🍳 | orders-api | Takes the order and gets the others to do their part | Orders aren't saved |
| 📒 | customers-api | Says who the customer is | Orders have no customer name |
| 💰 | billing-api | Makes the bill and takes payment | No bill is created |
| 📁 | documents-api | Uploads and downloads files | No documents can be attached |
| 🏃 | notification-worker | Emails the bill to the customer | The customer gets no email |
| 📚 | SQL Server | Keeps the permanent records | The system forgets everything |
| ⚡ | Redis | Gives back frequently asked things quickly | Still works, just slower |
| 📓 | MongoDB | Keeps a diary of what happened | No history of events |
| 📦 | MinIO | Stores the files | Files have nowhere to go |
| 📮 | Mailpit | Catches emails so you can see them | You can't see the emails |

---

## 1B. An everyday example: ordering a pizza 🍕

Imagine you phone a pizza shop. Our system does exactly the same steps:

| At the pizza shop | In our system | What happened |
|---|---|---|
| 1. You phone the shop | You click **"Create order"** on the website | A request is sent |
| 2. The receptionist passes your call to the right person | **Traefik** passes the request to **orders-api** | It reaches the right place |
| 3. The order-taker asks your name and checks the customer list | **orders-api** asks **customers-api** | The customer is identified |
| 4. The order is written down | The order is saved in **SQL Server** | A permanent record is made |
| 5. The cashier writes the bill | **billing-api** creates the invoice | A bill is made |
| 6. A little later you get a text: "order confirmed" | **notification-worker** sends an email | The customer is informed |
| 7. The shop notes it in its daybook | An audit record goes into **MongoDB** | The history is kept |
| 8. You call again "where's my order?" and get an instant answer | The answer comes from **Redis** (the cache) | A fast reply |

---

## 1C. How a new version goes live (picture) 🔄

When a programmer changes the code, it reaches customers like **new stock going from a factory to a shop**:

```
 👨‍💻 Programmer      🧪 Test          🏭 Factory         🏬 Warehouse      🏪 Shop
  changes code ──►  "is it OK?" ──►  packs a box  ──►  stores the box ──►  takes old stock
                     ✅ or ❌         (docker build)    (registry)         off the shelf,
                                                                          puts new stock on
                     ❌ = stop here!                                       (Swarm)
```

**What if the new stock is faulty?** Swarm notices it's "sick" and **puts the old stock back on the shelf** by itself. This is called a **rollback**.

```
   Old version ✅  ──►  New version 🤒 (sick)  ──►  Swarm: "put it back!"  ──►  Old version ✅
```

---

## 1D. Docker in 30 seconds 📦

```
  📜 Recipe            📦 Sealed box         🍱 Open box (in use)
  (Dockerfile)   ──►   (Image)         ──►   (Container)
                       made once             one box can be used
                                             for several copies
                                             (orders-api runs 2 copies)

  👔 Manager (Swarm):  "there must always be 2 copies running"
     one copy crashes?  ──►  the manager starts a new one straight away ✅
```

| Word | Kitchen example |
|---|---|
| Dockerfile | The **recipe** |
| Image | A **ready meal in the freezer** |
| Container | That meal **heated up and on a plate** (running) |
| Registry | The **freezer** where all the ready meals are kept |
| Swarm | The **host** who makes sure every guest always has a full plate |

---

## 2. The restaurant example 🍽️

Think of the whole system as a **restaurant**. Each part matches something in a restaurant:

| In a restaurant | In the lab | Job |
|---|---|---|
| Customer | **You (browser)** | Places the order |
| Menu | **web-portal** (website) | What the customer sees and clicks |
| Front door and host | **Traefik** | Sends everyone to the right place |
| Waiter | **orders-api** | Takes the order and gets things done |
| Guest book | **customers-api** | Remembers customers' names and emails |
| Cashier | **billing-api** | Makes the bill, takes payment |
| Filing cabinet | **documents-api** | Keeps paperwork (files) |
| Messenger who delivers the bill | **notification-worker** | Every 30 seconds, emails new bills |
| Manager's checklist | **admin-node** | Checks that all the staff are working |
| Main records book | **SQL Server** | Important, permanent data (orders, bills) |
| Things ready on the counter | **Redis** | Data needed quickly (cache) |
| Daybook | **MongoDB** | A record of what happened (audit log) |
| Storeroom | **MinIO** | Large files (PDFs, images) |
| Post office | **Mailpit** | Emails (in the lab, no real email leaves; you see it here) |
| Lunch box | **Docker container** | Each service packed in its own box with everything it needs |
| Restaurant manager | **Docker Swarm** | Runs the lunch boxes, replaces any that break |
| Manager's screen | **Portainer** | See and control everything on a screen |
| Warehouse | **Registry** | Where the ready lunch boxes (images) are stored |
| Factory | **Pipeline** (Azure DevOps / ci.sh) | Turns code into lunch boxes and delivers them |

---

## 3. Each part in simple words (with examples)

### 3.1 web-portal (the Angular website)
**What it is:** The website you open in your browser.
**Example:** In the "New order" form you enter **Sara Ahmed, Widget, 10, 5.00** and click the button.
**Remember:** The website never touches the database itself. It only asks **orders-api**: "please save this order".

### 3.2 Traefik (the front door)
**What it is:** Every request reaches Traefik first. It looks at the address and sends it to the right service.
**Example:**

```
/api/orders      → orders-api      (waiter)
/api/customers   → customers-api   (guest book)
/api/billing     → billing-api     (cashier)
/api/documents   → documents-api   (filing cabinet)
/admin           → admin-node      (manager's checklist)
/                → web-portal      (menu)
```

**Where this is written:** in `infra/stack.yml`, the `traefik...rule=PathPrefix(...)` lines under each service.

### 3.3 orders-api (the waiter): the most important service
**What it is:** The service that takes orders. It gets the other services to do their part.
**Example:** When you place an order, it does 3 things:
1. Asks customers-api: "who is customer no. 2?" → answer: **Sara Ahmed**
2. Saves the order in SQL Server and works out the total: 10 × 5.00 = 50.00, with **5% off for 10 or more items**, so **47.50**
3. Tells billing-api: "make a bill for 47.50"

**File:** `src/orders-api/Program.cs`

### 3.4 customers-api (the guest book)
**What it is:** Keeps the list of customers. It starts with 3: Ali Khan, Sara Ahmed, Bilal Hussain.
**Good to know:** orders-api talks to it using **gRPC** (see 3.14).
**File:** `src/customers-api/CustomerGrpcService.cs`

### 3.5 billing-api (the cashier)
**What it is:** Creates bills (invoices) and takes payment when you click "Pay".
**Example:** For order 1 it creates **Invoice 1: 47.50**. Payment is always "approved", because this is a **pretend** payment gateway, just for learning.
**File:** `src/billing-api/BillingGrpcService.cs`

### 3.6 documents-api (the filing cabinet)
**What it is:** Uploads and downloads files.
**Example:** You upload `invoice.pdf`:
- the **file itself** goes to MinIO (the storeroom),
- and SQL Server only gets a **note**: "order 1's file is in the storeroom at `2026/10/abc-invoice.pdf`".

**File:** `src/documents-api/Program.cs`

### 3.7 notification-worker (the messenger)
**What it is:** It has no website. It works in the background, and **every 30 seconds** it:
1. Checks: "is there a new bill that hasn't been emailed?" (SQL Server)
2. If yes, emails the customer (Mailpit)
3. Marks the bill as "email sent"
4. Writes in the diary (MongoDB): "Invoice 1 sent to sara@lab.local"

**Example:** About 30 seconds after you place an order, the portal shows **"Email sent: yes"**, and the email appears in Mailpit (port 8025).
**File:** `src/notification-worker/InvoiceEmailWorker.cs`

### 3.8 admin-node (the manager's checklist)
**What it is:** A page that asks every service "are you OK?" and shows the answers (healthy / DOWN).
**Where to see it:** add `/admin` to the end of the portal address.
**File:** `src/admin-node/server.js`

### 3.9 SQL Server (the main records book)
**What it is:** The real, permanent data lives here. The lab has 3 databases:

| Database | Owned by | What's in it |
|---|---|---|
| **SalesDb** | orders-api **and** billing-api (shared) | Orders + invoices |
| **CustomersDb** | customers-api | Customers |
| **DocumentsDb** | documents-api | Notes about where files are |

**Watch out:** SalesDb is **shared by two services**. The real system does this too. The risk: if one service changes a table, the other can break.

**SQL Agent:** an "alarm clock" inside SQL Server that runs jobs on a schedule. Example: "every night at 2 AM, clear out old orders". (See the `db/agent-jobs/` folder.)

### 3.10 Redis (things ready on the counter)
**What it is:** Very fast short-term memory. Things that are asked for again and again are kept here for a short time (60 seconds).
**Example:**
- First time you open order 1's "Details" → not in Redis (**MISS**) → fetched from SQL Server → saved in Redis
- Second time → found in Redis (**HIT**) → instant answer

**Remember:** If Redis stops, the system still works, just a little slower.

### 3.11 MongoDB (the diary)
**What it is:** Another kind of database, where each record can have a different shape. In the lab it holds the **audit log**, a history of what happened.

### 3.12 MinIO (the storeroom)
**What it is:** A place to store files. A folder here is called a **bucket** (the lab's bucket is called `documents`).

### 3.13 Mailpit (a pretend post office)
**What it is:** A server that "catches" emails. No real email leaves; they all appear in its inbox (port **8025**).

### 3.14 REST and gRPC (two ways of talking)

| | REST | gRPC |
|---|---|---|
| Who uses it | Website → services | Service → service |
| Example | **A letter:** easy to read, a bit slower | **A phone call:** fast, but both sides must agree on the language first |
| In the lab | Portal → `/api/orders` | orders-api → customers-api (port 8081) |

The "agreed language" for gRPC is written in `src/shared-protos/customers.proto`. Change it the wrong way, and the services can no longer understand each other (Exercise E4).

---

## 4. The Docker world in simple words

### Image and container
- **Image** = a sealed lunch box (the code plus everything it needs to run). Made once.
- **Container** = opening the lunch box and eating, that is, **running** the image.

**Example:** `orders-api:1` is an image. **2 containers** run from it (two waiters with the same training).

### Dockerfile
The **recipe** for making an image. Example: `src/orders-api/Dockerfile`
- **Part one:** compile the code on a big machine (the SDK)
- **Part two:** put only the finished program into a small box

### Registry (the warehouse)
Built images are stored here (port 5000). In the real system this is **Azure Container Registry (ACR)**.

### Docker Swarm (the restaurant manager)
- You tell Swarm: "run **2** containers of orders-api".
- One container dies → Swarm **starts a new one by itself**.
- A new version arrives → Swarm swaps them **one at a time**, so the website never goes down.
- The new version is bad → Swarm can **bring back the old version** (rollback).

### Stack
**One list (file)** of the whole system: `infra/stack.yml`. One command starts everything.

### Portainer
A way to see Swarm **on a screen** (port 9443). The same things you do with commands, done by clicking.

---

## 5. The journey of one order (a real example)

This is exactly what happened when the lab was tested:

```
1. You place an order on the portal: Sara Ahmed, Widget, 10 × 5.00
        │
2.      ▼  Traefik sees "/api/orders" → sends it to orders-api
        │
3.      ▼  orders-api ──gRPC──► customers-api: "who is customer 2?"  → "Sara Ahmed, sara@lab.local"
        │
4.      ▼  orders-api works out the total: 50.00 − 5% = 47.50, and saves Order 1 in SalesDb
        │
5.      ▼  orders-api ──gRPC──► billing-api: "make a bill for 47.50" → Invoice 1
        │
6.      ▼  The portal shows: "Order 1 created (total 47.50), invoice 1 raised"
        │
   ... about 30 seconds later ...
        │
7.      ▼  notification-worker: finds the new bill → emails Sara (Mailpit) → records it in MongoDB
        │
8.      ▼  On the portal, Invoice 1 now shows: "Email sent: yes"
```

Then you click "Details" → orders-api checks **Redis** first (MISS), fetches from SQL, saves it in Redis. Click again → **HIT**.

---

## 6. A code example, line by line

This is the real part of `src/orders-api/Program.cs` (shortened a little). Each line has a plain-English comment above it:

```csharp
// When someone sends POST /api/orders (a new order), this code runs
api.MapPost("/", async (CreateOrderRequest request, OrdersDbContext db, ...) =>
{
    // 1. Check: are the quantity and price sensible?
    if (request.Quantity <= 0 || request.UnitPrice <= 0)
        return Results.BadRequest("...");          // if not, reply with error 400

    // 2. Ask customers-api over gRPC: who is this customer?
    customer = await customers.GetCustomerAsync(new GetCustomerRequest { Id = request.CustomerId });

    // 3. Create the new order; work out the total using OrderPricing (the discount rule)
    var order = new Order
    {
        CustomerId = request.CustomerId,
        Product    = request.Product,
        Quantity   = request.Quantity,
        UnitPrice  = request.UnitPrice,
        Total      = OrderPricing.CalculateTotal(request.Quantity, request.UnitPrice),
    };

    // 4. Save it in SQL Server (SalesDb)
    db.Orders.Add(order);
    await db.SaveChangesAsync();

    // 5. Tell billing-api over gRPC: make a bill
    var invoice = await billing.CreateInvoiceAsync(new CreateInvoiceRequest { OrderId = order.Id, ... });

    // 6. Reply with the order number, the total, and the bill number
    return Results.Created(..., new { order.Id, order.Total, invoiceId = invoice.InvoiceId });
});
```

**The discount rule** (`src/orders-api/OrderPricing.cs`):

```csharp
var total = quantity * unitPrice;          // 10 × 5.00 = 50.00
if (quantity >= 10)                        // 10 or more?
    total -= total * 0.05m;                // 5% off: 50.00 − 2.50 = 47.50
```

And `tests/Orders.Tests/OrderPricingTests.cs` has **tests** that check this sum is always right. If someone changes the rule by mistake, a test fails and the pipeline stops.

---

## 7. From code to a running service (how a deploy works)

When you change code, it reaches the website like this:

```
1. Test      → is the code right?                (dotnet test)
2. Build     → make a new lunch box from recipe  (docker build)
3. Push      → put the box in the warehouse      (docker push → registry)
4. Deploy    → Swarm swaps old for new           (docker service update)
5. Check     → ask the website "are you OK?"     (smoke test)
```

**One command** does all five:

```bash
bash scripts/ci.sh orders-api
```

In the real system, an **Azure DevOps pipeline** does the same job (the `pipelines/` folder).

---

## 8. Everyday commands (in the Codespace terminal)

| Command | What it means |
|---|---|
| `docker stack services lab` | List all services and whether they're running (`1/1` = OK) |
| `bash scripts/status.sh` | A full health report (`200` = OK) |
| `docker service logs lab_orders-api` | orders-api's "diary": what it's been doing |
| `bash scripts/ci.sh orders-api` | Test, build and deploy a new version of orders-api |
| `docker service rollback lab_orders-api` | Bring back orders-api's **previous** version |
| `bash scripts/sql.sh -d SalesDb -Q "SELECT * FROM orders.Orders"` | Look at the orders in the database |

---

## 9. Which file should I open?

| To understand… | Open this file |
|---|---|
| The whole system at a glance | `infra/stack.yml` |
| How an order is created | `src/orders-api/Program.cs` |
| The discount sum | `src/orders-api/OrderPricing.cs` |
| The cache (Redis) | `src/orders-api/OrderCache.cs` |
| What services say to each other | `src/shared-protos/*.proto` |
| Where customer details come from | `src/customers-api/CustomerGrpcService.cs` |
| How a bill is made | `src/billing-api/BillingGrpcService.cs` |
| How the email is sent | `src/notification-worker/InvoiceEmailWorker.cs` |
| File uploads | `src/documents-api/Program.cs` |
| The website's buttons | `src/web-portal/src/app/app.html` |
| How an image is built | `src/orders-api/Dockerfile` |
| The deploy steps | `scripts/ci.sh` |

---

## 10. Glossary: hard words made simple

| Word | Simple meaning |
|---|---|
| **Service** | A small program that does one job (like a waiter) |
| **Microservices** | A big system made of many small services |
| **API** | A service's "counter", where others ask it to do things |
| **Endpoint** | One specific counter window, like `/api/orders` |
| **Request / Response** | Question / Answer |
| **Database** | A book of records for data |
| **Schema** | A section inside a database (like `orders` and `billing`) |
| **Migration** | A written-down way (in code) of changing a database's structure |
| **Cache** | Quick short-term memory (Redis) |
| **Image** | A sealed lunch box (a ready-to-run program) |
| **Container** | A lunch box in use (a running program) |
| **Replica** | Another copy of the same service (a second waiter) |
| **Deploy** | Putting a new version live |
| **Rollback** | Going back to the previous version |
| **Pipeline** | The test → build → deploy factory |
| **Health check** | The "are you OK?" question Swarm asks every 10 seconds |
| **Secret** | Something hidden, like a password |
| **Drift** | When the running system and what's written in Git no longer match |
| **Git / GitHub** | A record of every code change, and the website where it's stored |
| **Codespace** | Your computer in the cloud from GitHub, used in a browser |

---

## 11. What next? (3 small tasks)

**Task 1: Follow an order yourself (10 minutes)**
1. On the portal (port 8000), place a new order: **Ali Khan, 12 Widgets, 10.00 each**
2. Guess the total first (hint: 12 × 10 = 120, then 5% off). Answer: **114.00**
3. Click "Details", then find the email in Mailpit (port 8025).

**Task 2: Watch the system heal itself (5 minutes)**
In the terminal:
```bash
docker rm -f $(docker ps -q -f name=lab_orders-api | head -n 1)
```
Then:
```bash
docker service ps lab_orders-api
```
Notice: Swarm started a new container **by itself**.

**Task 3: Watch a rollback (5 minutes)**
```bash
docker service update --env-add FORCE_UNHEALTHY=true lab_orders-api
```
This releases a "broken" version. Swarm sees it is sick and **brings back the old version by itself**. Check:
```bash
docker service inspect lab_orders-api --format '{{.UpdateStatus.State}}'
```
The answer should be `rollback_completed`.

---

> If anything doesn't make sense, send the **file name** or a **screenshot**, and it can be explained even more simply.
> Once this guide makes sense, move on to **section 10** (exercises) of the full guide, `01-How-The-Application-Works.md`.
