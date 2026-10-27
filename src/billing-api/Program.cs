using Billing.Api;
using Billing.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);

// billing-api shares SalesDb with orders-api, but owns only the "billing" schema
// and keeps its own migration history table there.
builder.Services.AddDbContext<BillingDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Sales"),
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", BillingDbContext.Schema)));

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

var app = builder.Build();

await app.MigrateWithRetryAsync<BillingDbContext>();

var version = app.Configuration["APP_VERSION"] ?? "v1";

app.MapGrpcService<BillingGrpcService>();
app.MapGrpcReflectionService();

app.MapGet("/health", () => Results.Ok("healthy"));

var api = app.MapGroup("/api/billing");

api.MapGet("/version", () => new { service = "billing-api", version });

api.MapGet("/invoices", async (BillingDbContext db) =>
    await db.Invoices.OrderByDescending(i => i.Id).Take(50).ToListAsync());

// Stand-in for the external payment gateway: it always approves.
api.MapPost("/invoices/{id:int}/pay", async (int id, BillingDbContext db) =>
{
    var invoice = await db.Invoices.FindAsync(id);
    if (invoice is null)
        return Results.NotFound();
    if (invoice.Paid)
        return Results.Conflict($"Invoice {id} is already paid.");

    invoice.Paid = true;
    await db.SaveChangesAsync();
    return Results.Ok(new { approved = true, gatewayReference = $"FAKE-{Guid.NewGuid():N}"[..17], invoice.Id });
});

app.Run();
