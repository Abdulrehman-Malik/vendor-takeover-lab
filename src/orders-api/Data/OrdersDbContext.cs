using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Orders.Api.Data;

/// <summary>
/// orders-api owns the "orders" schema inside the SHARED SalesDb database.
/// billing-api owns the "billing" schema in the same database.
/// </summary>
public class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public const string Schema = "orders";

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<Order>(e =>
        {
            e.Property(o => o.Product).HasMaxLength(200);
            e.Property(o => o.UnitPrice).HasPrecision(18, 2);
            e.Property(o => o.Total).HasPrecision(18, 2);
        });
    }
}

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Product { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Lets "dotnet ef migrations add" run without a live database.</summary>
public class OrdersDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    public OrdersDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<OrdersDbContext>()
            .UseSqlServer("Server=localhost;Database=SalesDb;Integrated Security=true;TrustServerCertificate=True",
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", OrdersDbContext.Schema))
            .Options);
}
