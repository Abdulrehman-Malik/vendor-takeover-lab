using Microsoft.EntityFrameworkCore;

namespace Documents.Api.Data;

public static class MigrationExtensions
{
    /// <summary>
    /// Applies EF Core migrations at start-up, retrying while SQL Server is still starting.
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
