using MongoDB.Driver;
using Notification.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);

builder.Services.AddSingleton<IMongoClient>(_ =>
    new MongoClient(builder.Configuration.GetConnectionString("Mongo")));
builder.Services.AddHostedService<InvoiceEmailWorker>();

builder.Build().Run();
