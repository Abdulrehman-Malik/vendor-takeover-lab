using Billing.Api.Data;
using Grpc.Core;
using Lab.Contracts.Billing;

namespace Billing.Api;

public class BillingGrpcService(BillingDbContext db, ILogger<BillingGrpcService> log)
    : BillingService.BillingServiceBase
{
    public override async Task<CreateInvoiceReply> CreateInvoice(CreateInvoiceRequest request, ServerCallContext context)
    {
        var invoice = new Invoice
        {
            OrderId = request.OrderId,
            CustomerEmail = request.CustomerEmail,
            Amount = (decimal)request.Amount,
            CreatedAt = DateTime.UtcNow,
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(context.CancellationToken);

        // The email is NOT sent here: notification-worker picks up invoices with EmailSent = 0.
        log.LogInformation("Invoice {InvoiceId} created for order {OrderId}", invoice.Id, invoice.OrderId);
        return new CreateInvoiceReply { InvoiceId = invoice.Id };
    }
}
