using System.Net.Mail;
using Microsoft.Data.SqlClient;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Notification.Worker;

/// <summary>
/// Background worker with no web endpoint. Every 30 seconds it:
///   1. reads invoices with EmailSent = 0 from the SHARED SalesDb (billing.Invoices),
///   2. emails each customer through SMTP (Mailpit in the lab),
///   3. marks the invoice as sent,
///   4. writes an audit document to MongoDB.
/// It does not own the billing schema; it relies on billing-api's column names staying the same.
/// </summary>
public class InvoiceEmailWorker(IConfiguration config, IMongoClient mongo, ILogger<InvoiceEmailWorker> log)
    : BackgroundService
{
    static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var audit = mongo.GetDatabase("labdb").GetCollection<BsonDocument>("audit");

        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await SendPendingInvoiceEmailsAsync(audit, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogWarning("Run failed, will retry in {Seconds}s: {Message}", Interval.TotalSeconds, ex.Message);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    async Task SendPendingInvoiceEmailsAsync(IMongoCollection<BsonDocument> audit, CancellationToken ct)
    {
        await using var sql = new SqlConnection(config.GetConnectionString("Sales"));
        await sql.OpenAsync(ct);

        var pending = new List<(int Id, int OrderId, string Email, decimal Amount)>();
        await using (var cmd = new SqlCommand(
            "SELECT TOP 20 Id, OrderId, CustomerEmail, Amount FROM billing.Invoices WHERE EmailSent = 0 ORDER BY Id", sql))
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
                pending.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetDecimal(3)));
        }

        if (pending.Count == 0)
            return;

        using var smtp = new SmtpClient(config["Smtp:Host"], config.GetValue("Smtp:Port", 25));
        foreach (var invoice in pending)
        {
            await smtp.SendMailAsync(new MailMessage(
                from: config["Smtp:From"] ?? "billing@lab.local",
                to: invoice.Email,
                subject: $"Invoice {invoice.Id} for order {invoice.OrderId}",
                body: $"Thank you for your order {invoice.OrderId}. Amount due: {invoice.Amount:0.00}."), ct);

            await using (var update = new SqlCommand("UPDATE billing.Invoices SET EmailSent = 1 WHERE Id = @id", sql))
            {
                update.Parameters.AddWithValue("@id", invoice.Id);
                await update.ExecuteNonQueryAsync(ct);
            }

            await audit.InsertOneAsync(new BsonDocument
            {
                ["event"] = "InvoiceEmailed",
                ["invoiceId"] = invoice.Id,
                ["orderId"] = invoice.OrderId,
                ["to"] = invoice.Email,
                ["at"] = DateTime.UtcNow,
                ["worker"] = Environment.MachineName,
            }, cancellationToken: ct);

            log.LogInformation("Emailed invoice {InvoiceId} to {Email}", invoice.Id, invoice.Email);
        }
    }
}
