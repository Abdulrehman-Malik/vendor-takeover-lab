# Lab ko Asaan Urdu Mein Samjhein

> **Yeh guide kis ke liye hai?** Us shakhs ke liye jo bilkul naya hai.
> Har cheez **asaan alfaz** aur **misaal (example)** ke saath samjhayi gayi hai.
> Technical alfaz English mein hi rakhe gaye hain, kyunke code aur commands mein yahi alfaz aate hain.
>
> **Tip:** VS Code mein yeh file kholein aur **Ctrl + Shift + V** dabayein, to yeh achhi tarah formatted dikhegi.
>
> English mein parhna hai? [Easy-English-Guide.md](Easy-English-Guide.md) dekhein.

---

## 1. Yeh lab hai kya?

Ek company (vendor) ne ek bara software system banaya hua hai. Aap ki team ko yeh system **apne haath mein lena (takeover)** hai.

Asal system bohot bara hai (33 projects, 21 databases). Is liye humne usi tarah ka **ek chhota sa model** banaya hai, taake aap aaram se seekh sakein ke:

- system ke hisse (parts) kya hain,
- woh aapas mein kaise baat karte hain,
- code se chalti hui service kaise banti hai,
- aur vendor ne kahan "chupi hui" ghaltiyan chhori hain.

**Misaal:** Jaise pilot asli jahaz se pehle **simulator** par seekhta hai, waise hi aap asli system se pehle yeh lab chala kar seekhein ge.

---

## 1A. Ek tasveer mein poora system 🖼️

Pehle sirf yeh tasveer dekhein. Baqi sab baad mein.

```
                         🧑 AAP (browser)
                               │
                               │  "Mujhe 10 Widget chahiye"
                               ▼
                    ┌─────────────────────┐
                    │  🚪 DARWAZA          │   Traefik
                    │  "Aap kidhar jaana   │   (har cheez yahin se andar aati hai)
                    │   chahte hain?"      │
                    └──────────┬──────────┘
                               │
          ┌────────────────────┼────────────────────┐
          ▼                    ▼                    ▼
   ┌─────────────┐     ┌──────────────┐     ┌──────────────┐
   │ 🖥️ WEBSITE   │     │ 🧑‍🍳 WAITER    │     │ 📁 FILES      │
   │ web-portal  │     │ orders-api   │     │ documents-api│
   │ (jo aap     │     │ (order leta  │     │ (kaghaz      │
   │  dekhte hain)│     │  hai)        │     │  sambhalta)  │
   └─────────────┘     └──────┬───────┘     └──────┬───────┘
                              │                    │
                 ┌────────────┴─────────┐          │
                 ▼                      ▼          ▼
         ┌──────────────┐       ┌──────────────┐  ┌──────────────┐
         │ 📒 REGISTER   │       │ 💰 CASHIER    │  │ 📦 STORE ROOM │
         │ customers-api│       │ billing-api  │  │ MinIO        │
         │ "Yeh kaun    │       │ "Bill bana   │  │ (files yahan │
         │  customer?"  │       │  diya"       │  │  rakhi jaati)│
         └──────────────┘       └──────┬───────┘  └──────────────┘
                                       │
                                       ▼
                               ┌──────────────┐        ┌──────────────┐
                               │ 🏃 PEON       │ ─────► │ 📮 DAAK KHANA │
                               │ notification │  email │ Mailpit      │
                               │ -worker      │        └──────────────┘
                               └──────────────┘

   Sab ka data:   📚 SQL Server (bara register)   ⚡ Redis (jaldi wali yaad)   📓 MongoDB (diary)
```

### Har hisse ka maqsad (purpose), sirf ek line mein

| Tasveer | Naam | Is ka kaam ek line mein | Agar yeh na ho to? |
|---|---|---|---|
| 🚪 | Traefik | Har aane wale ko sahi khirki tak bhejna | Koi bhi service tak nahi pohnch sakta |
| 🖥️ | web-portal | Aap ko buttons aur form dikhana | Aap order hi nahi de sakte |
| 🧑‍🍳 | orders-api | Order lena aur baqi sab se kaam karwana | Order save nahi hoga |
| 📒 | customers-api | Batana ke customer kaun hai | Order par naam nahi aayega |
| 💰 | billing-api | Bill banana aur payment lena | Bill nahi banega |
| 📁 | documents-api | Files upload/download karna | Kaghazat attach nahi honge |
| 🏃 | notification-worker | Bill ki email bhejna | Customer ko email nahi jayegi |
| 📚 | SQL Server | Pakka record rakhna | Sab kuch bhool jayega |
| ⚡ | Redis | Baar baar wali cheez jaldi dena | System chalega, bas slow |
| 📓 | MongoDB | "Kya hua" ki diary | Hisaab-kitaab ka record nahi |
| 📦 | MinIO | Files rakhna | Files kahin nahi jaayengi |
| 📮 | Mailpit | Emails pakar kar dikhana | Emails dikhengi nahi |

---

## 1B. Rozmarra ki misaal: Pizza order karna 🍕

Socho aap phone par pizza order karte hain. Bilkul yahi hamare system mein hota hai:

| Pizza wali dukaan | Hamara system | Kya hua |
|---|---|---|
| 1. Aap phone karte hain | Aap website par **"Create order"** dabate hain | Request bheji |
| 2. Receptionist call sahi bande ko deta hai | **Traefik** request **orders-api** ko deta hai | Sahi jagah pohnchi |
| 3. Order taker poochta hai "aap ka naam?" aur register dekhta hai | **orders-api** **customers-api** se poochti hai | Customer pehchana gaya |
| 4. Order kaghaz par likha jata hai | Order **SQL Server** mein save hota hai | Pakka record bana |
| 5. Cashier bill banata hai | **billing-api** invoice banati hai | Bill bana |
| 6. Thori der baad SMS aata hai "order confirm" | **notification-worker** email bhejta hai | Customer ko pata chal gaya |
| 7. Dukaan ki diary mein likha jata hai | **MongoDB** mein audit record | Hisaab mehfooz |
| 8. Dobara call karo "mera order kahan?" to foran jawab | **Redis** se foran jawab (cache) | Tez jawab |

---

## 1C. Naya version kaise lagta hai? (tasveer) 🔄

Jab programmer code badalta hai, to woh customer tak aise pohnchta hai, jaise **factory se dukaan tak naya maal**:

```
 👨‍💻 Programmer        🧪 Test            🏭 Factory          🏬 Godaam          🏪 Dukaan
  code badla   ──►  "sahi hai?"   ──►  dabba banaya  ──►  dabba rakha  ──►  purana hataya,
                     ✅ ya ❌           (docker build)     (registry)       naya lagaya
                                                                           (Swarm)
                     ❌ ho to
                     yahin ruk jao!
```

**Agar naya maal kharab nikle?** Swarm khud dekh leta hai ("bimaar hai!") aur **purana maal wapas** laga deta hai. Isay **Rollback** kehte hain.

```
   Purana version ✅  ──►  Naya version 🤒 (bimaar)  ──►  Swarm: "wapas!"  ──►  Purana version ✅
```

---

## 1D. Docker ko 30 second mein samjhein 📦

```
  📜 Recipe            📦 Band dabba          🍱 Khula dabba (chal raha)
  (Dockerfile)   ──►   (Image)          ──►   (Container)
                       ek dafa banta          ek dabbe se kai copies
                                              chal sakti hain
                                              (orders-api ki 2 copies)

  👔 Manager (Swarm):  "2 copies chalni chahiye"
     ek copy gir gayi?  ──►  manager foran nayi chala deta hai ✅
```

| Lafz | Ghar ki misaal |
|---|---|
| Dockerfile | Biryani ki **recipe** |
| Image | **Freezer mein rakhi** tayyar biryani ka dabba |
| Container | Woh dabba **garam kar ke** plate mein (chal raha hai) |
| Registry | **Freezer**, jahan saare dabbe rakhe hain |
| Swarm | **Ghar ka manager**, jo dekhta hai har mehmaan ki plate bhari rahe |

---

## 2. Sab se aasaan misaal: Ek Restaurant 🍽️

Poore system ko ek **restaurant** samjhein. Har hissa restaurant ki kisi cheez jaisa hai:

| Restaurant mein | Lab mein | Kaam |
|---|---|---|
| Customer | **Aap (browser)** | Order deta hai |
| Menu card | **web-portal** (website) | Jo customer dekhta aur click karta hai |
| Darwaza aur receptionist | **Traefik** | Har aane wale ko sahi jagah bhejta hai |
| Order lene wala waiter | **orders-api** | Order leta hai aur aage kaam karwata hai |
| Customer register | **customers-api** | Customers ke naam aur email yaad rakhta hai |
| Cashier | **billing-api** | Bill (invoice) banata hai, payment leta hai |
| File cabinet | **documents-api** | Kaghazat (files) sambhalta hai |
| Peon jo bill ghar bhejta hai | **notification-worker** | Har 30 second baad naye bills email karta hai |
| Manager ki checking list | **admin-node** | Dekhta hai sab staff kaam kar raha hai ya nahi |
| Bara register / khaata | **SQL Server** | Pakka aur zaroori data (orders, bills) |
| Counter par rakhi ready cheezein | **Redis** | Jaldi chahiye wala data (cache) |
| Diary / roznamcha | **MongoDB** | Kya kya hua, uska record (audit log) |
| Store room | **MinIO** | Bari files (PDF, images) |
| Daak khana (post office) | **Mailpit** | Emails (lab mein asli email nahi jaati, sirf yahan dikhti hai) |
| Tiffin box | **Docker container** | Har service apne dabbe mein band, sab saaman ke saath |
| Restaurant manager | **Docker Swarm** | Tiffin box chalata hai, kharab ho jaye to naya laga deta hai |
| Manager ki screen | **Portainer** | Sab kuch screen par dekhne aur control karne ke liye |
| Godaam (warehouse) | **Registry** | Tayyar tiffin boxes (images) yahan rakhe jaate hain |
| Factory | **Pipeline** (Azure DevOps / ci.sh) | Code se tiffin box banati hai aur restaurant tak pohnchati hai |

---

## 3. Har hissa asaan alfaz mein (misaal ke saath)

### 3.1 web-portal (Angular website)
**Kya hai:** Woh website jo aap browser mein kholte hain.
**Misaal:** Aap "New order" form mein **Sara Ahmed, Widget, 10, 5.00** likh kar button dabate hain.
**Yaad rakhein:** Website khud database ko haath nahi lagati. Woh sirf **orders-api** se kehti hai "yeh order save karo".

### 3.2 Traefik (darwaza / receptionist)
**Kya hai:** Har request pehle Traefik ke paas aati hai, phir woh address dekh kar sahi service ko bhejta hai.
**Misaal:**

```
/api/orders      → orders-api      (waiter)
/api/customers   → customers-api   (register)
/api/billing     → billing-api     (cashier)
/api/documents   → documents-api   (file cabinet)
/admin           → admin-node      (manager ki list)
/                → web-portal      (menu card)
```

**Yeh kahan likha hai:** `infra/stack.yml` mein har service ke neeche `traefik...rule=PathPrefix(...)` wali lines.

### 3.3 orders-api (waiter) — sab se zaroori service
**Kya hai:** Order lene wali service. Yahi baqi services ko kaam deti hai.
**Misaal:** Jab aap order dete hain, yeh 3 kaam karti hai:
1. customers-api se poochti hai: "customer no. 2 kaun hai?" → jawab: **Sara Ahmed**
2. Order ko SQL Server mein save karti hai, aur total nikalti hai: 10 × 5.00 = 50.00, **10 ya zyada cheezon par 5% discount**, to **47.50**
3. billing-api se kehti hai: "47.50 ka bill bana do"

**File:** `src/orders-api/Program.cs`

### 3.4 customers-api (register)
**Kya hai:** Customers ki list rakhti hai. Isme 3 customers pehle se hain: Ali Khan, Sara Ahmed, Bilal Hussain.
**Khaas baat:** orders-api is se **gRPC** ke zariye baat karti hai (neeche 3.15 dekhein).
**File:** `src/customers-api/CustomerGrpcService.cs`

### 3.5 billing-api (cashier)
**Kya hai:** Bill (invoice) banati hai aur "Pay" button par payment leti hai.
**Misaal:** Order 1 ke liye **Invoice 1: 47.50** banti hai. Payment hamesha "approved" hoti hai, kyunke yeh **nakli (fake)** payment gateway hai, sirf seekhne ke liye.
**File:** `src/billing-api/BillingGrpcService.cs`

### 3.6 documents-api (file cabinet)
**Kya hai:** Files upload aur download karti hai.
**Misaal:** Aap `invoice.pdf` upload karte hain:
- **File** khud MinIO (store room) mein jaati hai,
- aur SQL Server mein sirf ek **parchi** likhi jaati hai: "order 1 ki file, store room mein `2026/10/abc-invoice.pdf` par rakhi hai".

**File:** `src/documents-api/Program.cs`

### 3.7 notification-worker (peon)
**Kya hai:** Is ki koi website nahi. Yeh background mein **har 30 second** baad check karta hai:
1. "Koi naya bill hai jiski email nahi gayi?" (SQL Server)
2. Agar hai, to customer ko email bhejta hai (Mailpit)
3. Bill par "email chali gayi" likh deta hai
4. Diary (MongoDB) mein likhta hai: "Invoice 1, sara@lab.local ko bheji"

**Misaal:** Order dene ke ~30 second baad portal mein **"Email sent: yes"** ho jata hai, aur Mailpit (port 8025) mein email dikhti hai.
**File:** `src/notification-worker/InvoiceEmailWorker.cs`

### 3.8 admin-node (manager ki checking list)
**Kya hai:** Ek page jo har service se poochta hai "theek ho?" aur jawab dikhata hai (healthy / DOWN).
**Kahan dekhein:** Portal ke address ke aakhir mein `/admin` lagayein.
**File:** `src/admin-node/server.js`

### 3.9 SQL Server (bara register)
**Kya hai:** Asal aur pakka data yahan rehta hai. Lab mein 3 databases hain:

| Database | Kis ka hai | Kya rakha hai |
|---|---|---|
| **SalesDb** | orders-api **aur** billing-api dono ka (shared) | Orders + Invoices |
| **CustomersDb** | customers-api | Customers |
| **DocumentsDb** | documents-api | Files ki parchiyan |

**Dhyaan dein:** SalesDb **do services mil kar** use karti hain. Asal system mein bhi aisa hai. Iska khatra: ek service table badle to doosri toot sakti hai.

**SQL Agent:** SQL Server ke andar ek "alarm clock" jo khud time par kaam karta hai. Misaal: "har raat 2 baje purane orders saaf karo". (`db/agent-jobs/` folder)

### 3.10 Redis (counter par rakhi cheezein)
**Kya hai:** Bohot tez yaad-dasht (memory). Jo cheez baar baar maangi jaye, woh yahan thori der (60 second) rakh di jaati hai.
**Misaal:**
- Pehli baar order 1 ki "Details" kholi → Redis mein nahi mila (**MISS**) → SQL Server se laya → Redis mein rakh diya
- Doosri baar → Redis mein mil gaya (**HIT**) → foran jawab

**Yaad rakhein:** Redis band ho jaye to system phir bhi chalta hai, bas thora slow.

### 3.11 MongoDB (diary)
**Kya hai:** Ek aur database, lekin isme har record ki shakal (shape) alag ho sakti hai. Lab mein yahan **audit log** hai, yani kya kya hua uska hisaab.

### 3.12 MinIO (store room)
**Kya hai:** Files rakhne ki jagah. Folder ko yahan **bucket** kehte hain (lab mein bucket ka naam `documents` hai).

### 3.13 Mailpit (nakli daak khana)
**Kya hai:** Email "pakarne" wala server. Koi asli email bahar nahi jaati; sab yahan inbox mein dikhti hain (port **8025**).

### 3.14 REST aur gRPC (baat karne ke do tareeqe)

| | REST | gRPC |
|---|---|---|
| Kaun use karta hai | Website → services | Service → service |
| Misaal | **Khat (letter):** parhne mein asaan, thora slow | **Phone call:** tez, lekin dono taraf pehle se tay zubaan |
| Lab mein | Portal → `/api/orders` | orders-api → customers-api (port 8081) |

gRPC ki "tay zubaan" `src/shared-protos/customers.proto` file mein likhi hai. Is file ko ghalat badla to services aapas mein baat nahi kar payengi (Exercise E4).

---

## 4. Docker ki duniya: asaan alfaz mein

### Image aur Container
- **Image** = band tiffin box (code + jo kuch chalne ke liye chahiye). Ek dafa banta hai.
- **Container** = tiffin box khol kar khana khana, yani image ko **chalana**.

**Misaal:** `orders-api:1` ek image hai. Usi se **2 containers** chal rahe hain (do waiter, ek hi training ke saath).

### Dockerfile
Image banane ki **recipe**. Misaal: `src/orders-api/Dockerfile`
- **Pehla hissa:** bari machine (SDK) par code compile karo
- **Doosra hissa:** sirf tayyar cheez ek chhote dabbe mein daal do

### Registry (godaam)
Banayi hui images yahan rakhi jaati hain (port 5000). Asal system mein yeh **Azure Container Registry (ACR)** hai.

### Docker Swarm (restaurant manager)
- Aap Swarm ko batate hain: "orders-api ke **2** container chalao".
- Ek container mar jaye → Swarm **khud naya** chala deta hai.
- Nayi version aaye → Swarm **ek ek kar ke** purane band aur naye chalu karta hai, taake website kabhi band na ho.
- Nayi version kharab ho → Swarm **purani version wapas** la sakta hai (rollback).

### Stack
Poore system ki **ek list (file)**: `infra/stack.yml`. Ek command se sab kuch chal jaata hai.

### Portainer
Swarm ko **screen par** dekhne ka tareeqa (port 9443). Wahi kaam jo commands se hota hai, yahan click se.

---

## 5. Ek order ka poora safar (asal misaal)

Yeh bilkul wahi hai jo lab test karte waqt hua:

```
1. Aap portal par order dete hain: Sara Ahmed, Widget, 10 × 5.00
        │
2.      ▼  Traefik dekhta hai "/api/orders" → orders-api ko bhejta hai
        │
3.      ▼  orders-api ──gRPC──► customers-api: "customer 2 kaun?"  → "Sara Ahmed, sara@lab.local"
        │
4.      ▼  orders-api total nikalti hai: 50.00 − 5% = 47.50, aur SalesDb mein Order 1 save karti hai
        │
5.      ▼  orders-api ──gRPC──► billing-api: "47.50 ka bill banao" → Invoice 1
        │
6.      ▼  Portal par dikhta hai: "Order 1 created (total 47.50), invoice 1 raised"
        │
   ... ~30 second baad ...
        │
7.      ▼  notification-worker: naya bill mila → Sara ko email (Mailpit) → MongoDB mein record
        │
8.      ▼  Portal par Invoice 1: "Email sent: yes"
```

Phir aap "Details" dabate hain → orders-api pehle **Redis** dekhti hai (MISS), SQL se laati hai, Redis mein rakh deti hai. Dobara dabayein → **HIT**.

---

## 6. Code ki ek misaal, line by line

Yeh `src/orders-api/Program.cs` ka asal hissa hai (thora chhota kiya hua). Har line ke upar Urdu mein matlab likha hai:

```csharp
// Jab koi POST /api/orders bheje (naya order), to yeh code chalega
api.MapPost("/", async (CreateOrderRequest request, OrdersDbContext db, ...) =>
{
    // 1. Check: quantity aur price theek hain?
    if (request.Quantity <= 0 || request.UnitPrice <= 0)
        return Results.BadRequest("...");          // ghalat ho to 400 error

    // 2. gRPC se customers-api se poocho: yeh customer kaun hai?
    customer = await customers.GetCustomerAsync(new GetCustomerRequest { Id = request.CustomerId });

    // 3. Naya order banao, total OrderPricing se nikalo (discount wala rule)
    var order = new Order
    {
        CustomerId = request.CustomerId,
        Product    = request.Product,
        Quantity   = request.Quantity,
        UnitPrice  = request.UnitPrice,
        Total      = OrderPricing.CalculateTotal(request.Quantity, request.UnitPrice),
    };

    // 4. SQL Server (SalesDb) mein save karo
    db.Orders.Add(order);
    await db.SaveChangesAsync();

    // 5. gRPC se billing-api ko kaho: bill banao
    var invoice = await billing.CreateInvoiceAsync(new CreateInvoiceRequest { OrderId = order.Id, ... });

    // 6. Jawab wapas bhejo: order number, total, aur bill number
    return Results.Created(..., new { order.Id, order.Total, invoiceId = invoice.InvoiceId });
});
```

**Discount ka rule** (`src/orders-api/OrderPricing.cs`):

```csharp
var total = quantity * unitPrice;          // 10 × 5.00 = 50.00
if (quantity >= 10)                        // 10 ya zyada?
    total -= total * 0.05m;                // 5% kam: 50.00 − 2.50 = 47.50
```

Aur `tests/Orders.Tests/OrderPricingTests.cs` mein **tests** hain jo check karte hain ke yeh hisaab hamesha sahi rahe. Agar koi rule ghalat badal de, test fail ho jayega aur pipeline ruk jayegi.

---

## 7. Code se chalti service tak (deploy kaise hota hai)

Jab aap code badalte hain, to woh website tak aise pohnchta hai:

```
1. Test      → kya code sahi hai?            (dotnet test)
2. Build     → recipe se naya tiffin box     (docker build)
3. Push      → tiffin box godaam mein        (docker push → registry)
4. Deploy    → Swarm purane ki jagah naya    (docker service update)
5. Check     → website se poocho "theek ho?" (smoke test)
```

Yeh paanchon kaam **ek command** karti hai:

```bash
bash scripts/ci.sh orders-api
```

Asal system mein yahi kaam **Azure DevOps pipeline** karti hai (`pipelines/` folder).

---

## 8. Rozana ke commands (Codespace terminal mein)

| Command | Matlab |
|---|---|
| `docker stack services lab` | Sab services ki list: kaun chal raha hai (`1/1` = theek) |
| `bash scripts/status.sh` | Poori sehat ki report (`200` = theek) |
| `docker service logs lab_orders-api` | orders-api kya kar rahi hai, uski "diary" |
| `bash scripts/ci.sh orders-api` | orders-api ka naya version test, build aur deploy karo |
| `docker service rollback lab_orders-api` | orders-api ka **purana** version wapas lao |
| `bash scripts/sql.sh -d SalesDb -Q "SELECT * FROM orders.Orders"` | Database mein orders dekho |

---

## 9. Kaun si file kholein? (jab kuch samajhna ho)

| Agar yeh samajhna hai… | To yeh file kholein |
|---|---|
| Poora system ek nazar mein | `infra/stack.yml` |
| Order kaise banta hai | `src/orders-api/Program.cs` |
| Discount ka hisaab | `src/orders-api/OrderPricing.cs` |
| Cache (Redis) | `src/orders-api/OrderCache.cs` |
| Services aapas mein kya baat karti hain | `src/shared-protos/*.proto` |
| Customer kahan se aata hai | `src/customers-api/CustomerGrpcService.cs` |
| Bill kaise banta hai | `src/billing-api/BillingGrpcService.cs` |
| Email kaise jaati hai | `src/notification-worker/InvoiceEmailWorker.cs` |
| File upload | `src/documents-api/Program.cs` |
| Website ke buttons | `src/web-portal/src/app/app.html` |
| Image kaise banti hai | `src/orders-api/Dockerfile` |
| Deploy ke steps | `scripts/ci.sh` |

---

## 10. Mushkil alfaz ki dictionary

| Lafz | Asaan matlab |
|---|---|
| **Service** | Ek chhota program jo ek kaam karta hai (jaise waiter) |
| **Microservices** | Bara system jo bohot si chhoti services se bana ho |
| **API** | Service ka "counter", jahan se doosre usse kaam karwate hain |
| **Endpoint** | API ka ek khaas darwaza, jaise `/api/orders` |
| **Request / Response** | Sawal / Jawab |
| **Database** | Data ka register |
| **Schema** | Database ke andar ek hissa (jaise `orders` aur `billing`) |
| **Migration** | Database ka structure badalne ka likha hua tareeqa (code mein) |
| **Cache** | Jaldi wali yaad-dasht (Redis) |
| **Image** | Band tiffin box (tayyar program) |
| **Container** | Chalta hua tiffin box |
| **Replica** | Ek hi service ki doosri copy (doosra waiter) |
| **Deploy** | Naya version chalu karna |
| **Rollback** | Purana version wapas lana |
| **Pipeline** | Test → build → deploy ki factory |
| **Health check** | "Theek ho?" ka sawal jo Swarm har 10 second poochta hai |
| **Secret** | Password jaisi chhupi cheez |
| **Drift** | Jab chalta hua system aur Git mein likhi cheez alag alag ho jayein |
| **Git / GitHub** | Code ka record aur uski online jagah |
| **Codespace** | GitHub par aap ka cloud computer, browser mein |

---

## 11. Ab kya karein? (3 chhote kaam)

**Kaam 1: Order ka safar khud dekhein (10 minute)**
1. Portal (port 8000) par naya order dein: **Ali Khan, 12 Widgets, 10.00 each**
2. Andaza lagayein total kya hoga (hint: 12 × 10 = 120, phir 5% kam). Jawab: **114.00**
3. "Details" dabayein, phir Mailpit (port 8025) mein email dhoondein.

**Kaam 2: Self-healing dekhein (5 minute)**
Terminal mein:
```bash
docker rm -f $(docker ps -q -f name=lab_orders-api | head -n 1)
```
Phir:
```bash
docker service ps lab_orders-api
```
Dekhein: Swarm ne **khud** naya container chala diya.

**Kaam 3: Rollback dekhein (5 minute)**
```bash
docker service update --env-add FORCE_UNHEALTHY=true lab_orders-api
```
Yeh ek "kharab" version deta hai. Swarm dekh leta hai ke yeh bimaar hai aur **khud** purana version wapas le aata hai. Check karein:
```bash
docker service inspect lab_orders-api --format '{{.UpdateStatus.State}}'
```
Jawab `rollback_completed` aana chahiye.

---

> Koi bhi cheez samajh na aaye to woh **file ka naam** ya **screenshot** bhejein, main usi ko aur asaan kar ke samjha dunga.
> Jab yeh guide samajh aa jaye, to bari guide `01-How-The-Application-Works.md` ke **section 10** (exercises) par jaayein.
