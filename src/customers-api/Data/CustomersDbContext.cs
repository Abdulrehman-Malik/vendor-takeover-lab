using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Customers.Api.Data;

/// <summary>customers-api has its own database, CustomersDb, which nobody else touches.</summary>
public class CustomersDbContext(DbContextOptions<CustomersDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(100);
            e.Property(c => c.Email).HasMaxLength(200);
            e.HasData(
                new Customer { Id = 1, Name = "Ali Khan", Email = "ali@lab.local" },
                new Customer { Id = 2, Name = "Sara Ahmed", Email = "sara@lab.local" },
                new Customer { Id = 3, Name = "Bilal Hussain", Email = "bilal@lab.local" });
        });
    }
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

/// <summary>Lets "dotnet ef migrations add" run without a live database.</summary>
public class CustomersDbContextFactory : IDesignTimeDbContextFactory<CustomersDbContext>
{
    public CustomersDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<CustomersDbContext>()
            .UseSqlServer("Server=localhost;Database=CustomersDb;Integrated Security=true;TrustServerCertificate=True")
            .Options);
}
