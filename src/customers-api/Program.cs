using Customers.Api;
using Customers.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);

builder.Services.AddDbContext<CustomersDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Customers")));

builder.Services.AddGrpc();
// Reflection lets tools like grpcurl list and call the service without the .proto file.
builder.Services.AddGrpcReflection();

var app = builder.Build();

await app.MigrateWithRetryAsync<CustomersDbContext>();

var version = app.Configuration["APP_VERSION"] ?? "v1";

// Port 8081 (HTTP/2): gRPC for other services. Port 8080 (HTTP/1.1): REST + health. See appsettings.json.
app.MapGrpcService<CustomerGrpcService>();
app.MapGrpcReflectionService();

app.MapGet("/health", () => Results.Ok("healthy"));

app.MapGet("/api/customers", async (CustomersDbContext db) =>
    await db.Customers.OrderBy(c => c.Id).ToListAsync());

app.MapGet("/api/customers/version", () => new { service = "customers-api", version });

app.Run();
