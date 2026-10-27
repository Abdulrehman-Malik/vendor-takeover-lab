using System.Text.Json;
using Grpc.Core;
using Lab.Contracts.Billing;
using Lab.Contracts.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Orders.Api;
using Orders.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Swarm secrets are mounted as files in /run/secrets. A secret named
// "ConnectionStrings__Sales" becomes the setting ConnectionStrings:Sales and
// overrides any environment variable with the same name.
builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);

builder.Services.AddDbContext<OrdersDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Sales"),
        sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", OrdersDbContext.Schema)));

builder.Services.AddStackExchangeRedisCache(o =>
    o.Configuration = builder.Configuration.GetConnectionString("Redis"));
builder.Services.AddSingleton<OrderCache>();

// gRPC clients: orders-api calls customers-api and billing-api over HTTP/2.
builder.Services.AddGrpcClient<CustomerService.CustomerServiceClient>(o =>
    o.Address = new Uri(builder.Configuration["Services:CustomersGrpc"]!));
builder.Services.AddGrpcClient<BillingService.BillingServiceClient>(o =>
    o.Address = new Uri(builder.Configuration["Services:BillingGrpc"]!));

var app = builder.Build();

await app.MigrateWithRetryAsync<OrdersDbContext>();

var version = app.Configuration["APP_VERSION"] ?? "v1";

// Used by the Swarm healthcheck. Set FORCE_UNHEALTHY=true to simulate a broken release (exercise E5).
app.MapGet("/health", (IConfiguration cfg) =>
    cfg.GetValue<bool>("FORCE_UNHEALTHY") ? Results.StatusCode(500) : Results.Ok("healthy"));

var api = app.MapGroup("/api/orders");

api.MapGet("/version", () => new { service = "orders-api", version });

api.MapGet("/whoami", () => new { container = Environment.MachineName, version });

api.MapGet("/", async (OrdersDbContext db) =>
    await db.Orders.OrderByDescending(o => o.Id).Take(50).ToListAsync());

api.MapGet("/{id:int}", async (
    int id,
    OrdersDbContext db,
    OrderCache cache,
    CustomerService.CustomerServiceClient customers) =>
{
    var cached = await cache.GetAsync(id);
    if (cached is not null)
        return Results.Content(cached, "application/json");

    var order = await db.Orders.FindAsync(id);
    if (order is null)
        return Results.NotFound();

    var customer = await customers.GetCustomerAsync(new GetCustomerRequest { Id = order.CustomerId });

    var details = new OrderDetails(
        order.Id, order.CustomerId, customer.Name, customer.Email,
        order.Product, order.Quantity, order.UnitPrice, order.Total,
        order.CreatedAt, ServedBy: Environment.MachineName);

    var json = JsonSerializer.Serialize(details, JsonSerializerOptions.Web);
    await cache.SetAsync(id, json);
    return Results.Content(json, "application/json");
});

api.MapPost("/", async (
    CreateOrderRequest request,
    OrdersDbContext db,
    OrderCache cache,
    CustomerService.CustomerServiceClient customers,
    BillingService.BillingServiceClient billing) =>
{
    if (request.Quantity <= 0 || request.UnitPrice <= 0 || string.IsNullOrWhiteSpace(request.Product))
        return Results.BadRequest("Product is required, and quantity and unit price must be positive.");

    CustomerReply customer;
    try
    {
        customer = await customers.GetCustomerAsync(new GetCustomerRequest { Id = request.CustomerId });
    }
    catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
    {
        return Results.BadRequest($"Customer {request.CustomerId} does not exist.");
    }

    var order = new Order
    {
        CustomerId = request.CustomerId,
        Product = request.Product.Trim(),
        Quantity = request.Quantity,
        UnitPrice = request.UnitPrice,
        Total = OrderPricing.CalculateTotal(request.Quantity, request.UnitPrice),
        CreatedAt = DateTime.UtcNow,
    };
    db.Orders.Add(order);
    await db.SaveChangesAsync();

    var invoice = await billing.CreateInvoiceAsync(new CreateInvoiceRequest
    {
        OrderId = order.Id,
        CustomerEmail = customer.Email,
        Amount = (double)order.Total,
    });

    await cache.RemoveAsync(order.Id);
    return Results.Created($"/api/orders/{order.Id}", new { order.Id, order.Total, invoiceId = invoice.InvoiceId });
});

app.Run();

record CreateOrderRequest(int CustomerId, string Product, int Quantity, decimal UnitPrice);

record OrderDetails(
    int Id, int CustomerId, string CustomerName, string CustomerEmail,
    string Product, int Quantity, decimal UnitPrice, decimal Total,
    DateTime CreatedAt, string ServedBy);
