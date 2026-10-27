using Microsoft.EntityFrameworkCore;

namespace Orders.Api.Data;

public static class MigrationExtensions
{
    /// <summary>
    /// Applies EF Core migrations at start-up (the "automated migrations" way).
    /// SQL Server can take ~30 seconds to start in a fresh stack, so retry instead of crashing.
    /// EF Core takes a database lock while migrating, so several replicas starting together is safe.
    /// </summary>
    public static async Task MigrateWithRetryAsync<TContext>(this WebApplication app, int maxAttempts = 24)
        where TContext : DbContext
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<TContext>().Database.MigrateAsync();
                app.Logger.LogInformation("Database migrations for {Context} applied", typeof(TContext).Name);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                app.Logger.LogWarning("Database not ready (attempt {Attempt}/{Max}): {Message}", attempt, maxAttempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }
}
