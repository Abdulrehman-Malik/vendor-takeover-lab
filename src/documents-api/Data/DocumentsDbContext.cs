using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Documents.Api.Data;

/// <summary>Stores only file METADATA. The file bytes live in MinIO under ObjectKey.</summary>
public class DocumentsDbContext(DbContextOptions<DocumentsDbContext> options) : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(e =>
        {
            e.Property(d => d.FileName).HasMaxLength(260);
            e.Property(d => d.ObjectKey).HasMaxLength(400);
            e.Property(d => d.ContentType).HasMaxLength(100);
        });
    }
}

public class Document
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string FileName { get; set; } = "";
    public string ObjectKey { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
}

/// <summary>Lets "dotnet ef migrations add" run without a live database.</summary>
public class DocumentsDbContextFactory : IDesignTimeDbContextFactory<DocumentsDbContext>
{
    public DocumentsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<DocumentsDbContext>()
            .UseSqlServer("Server=localhost;Database=DocumentsDb;Integrated Security=true;TrustServerCertificate=True")
            .Options);
}
