using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Documents.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);

builder.Services.AddDbContext<DocumentsDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Documents")));

// MinIO speaks the Amazon S3 protocol, so the normal AWS SDK works against it.
builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    var minio = builder.Configuration.GetSection("Minio");
    return new AmazonS3Client(
        new BasicAWSCredentials(minio["AccessKey"], minio["SecretKey"]),
        new AmazonS3Config
        {
            ServiceURL = minio["Endpoint"],
            ForcePathStyle = true,                       // http://minio:9000/<bucket>/<key>
            AuthenticationRegion = "us-east-1",
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        });
});

var app = builder.Build();

await app.MigrateWithRetryAsync<DocumentsDbContext>();

var bucket = app.Configuration["Minio:Bucket"] ?? "documents";
await EnsureBucketAsync(app, bucket);

app.MapGet("/health", () => Results.Ok("healthy"));

var api = app.MapGroup("/api/documents");

api.MapGet("/", async (DocumentsDbContext db) =>
    await db.Documents.OrderByDescending(d => d.Id).Take(50).ToListAsync());

api.MapPost("/", async ([FromForm] int orderId, IFormFile file, DocumentsDbContext db, IAmazonS3 s3) =>
{
    if (file.Length == 0)
        return Results.BadRequest("The file is empty.");

    // 1. The FILE goes to MinIO...
    var key = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}-{Path.GetFileName(file.FileName)}";
    await using (var stream = file.OpenReadStream())
    {
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = stream,
            ContentType = file.ContentType,
        });
    }

    // 2. ...and only a small RECORD pointing to it goes to SQL Server.
    var document = new Document
    {
        OrderId = orderId,
        FileName = Path.GetFileName(file.FileName),
        ObjectKey = key,
        ContentType = file.ContentType,
        SizeBytes = file.Length,
        UploadedAt = DateTime.UtcNow,
    };
    db.Documents.Add(document);
    await db.SaveChangesAsync();

    return Results.Created($"/api/documents/{document.Id}/download", document);
}).DisableAntiforgery();

api.MapGet("/{id:int}/download", async (int id, DocumentsDbContext db, IAmazonS3 s3) =>
{
    var document = await db.Documents.FindAsync(id);
    if (document is null)
        return Results.NotFound($"No document record {id} in SQL Server.");

    try
    {
        var obj = await s3.GetObjectAsync(bucket, document.ObjectKey);
        return Results.Stream(obj.ResponseStream, document.ContentType, document.FileName);
    }
    catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        // The database and the file store are out of sync (exercise E8).
        return Results.Problem(
            $"SQL Server has a record for '{document.ObjectKey}', but the file is missing from MinIO bucket '{bucket}'.",
            statusCode: StatusCodes.Status404NotFound);
    }
});

app.Run();

static async Task EnsureBucketAsync(WebApplication app, string bucket)
{
    var s3 = app.Services.GetRequiredService<IAmazonS3>();
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            if (!await AmazonS3Util.DoesS3BucketExistV2Async(s3, bucket))
                await s3.PutBucketAsync(bucket);
            app.Logger.LogInformation("MinIO bucket {Bucket} is ready", bucket);
            return;
        }
        catch (Exception ex) when (attempt < 24)
        {
            app.Logger.LogWarning("MinIO not ready (attempt {Attempt}/24): {Message}", attempt, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}
