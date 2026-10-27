using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Billing.Api.Data;

/// <summary>
/// billing-api owns the "billing" schema inside the SHARED SalesDb database.
/// notification-worker also reads and updates billing.Invoices with plain SQL,
/// so renaming a column here would silently break the worker.
/// </summary>
public class BillingDbContext(DbContextOptions<BillingDbContext> options) : DbContext(options)
{
    public const string Schema = "billing";

    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<Invoice>(e =>
        {
            e.Property(i => i.CustomerEmail).HasMaxLength(200);
            e.Property(i => i.Amount).HasPrecision(18, 2);
        });
    }
}

public class Invoice
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string CustomerEmail { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool EmailSent { get; set; }
    public bool Paid { get; set; }
}

/// <summary>Lets "dotnet ef migrations add" run without a live database.</summary>
public class BillingDbContextFactory : IDesignTimeDbContextFactory<BillingDbContext>
{
    public BillingDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<BillingDbContext>()
            .UseSqlServer("Server=localhost;Database=SalesDb;Integrated Security=true;TrustServerCertificate=True",
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", BillingDbContext.Schema))
            .Options);
}
