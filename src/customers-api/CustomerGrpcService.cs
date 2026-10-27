using Customers.Api.Data;
using Grpc.Core;
using Lab.Contracts.Customers;

namespace Customers.Api;

public class CustomerGrpcService(CustomersDbContext db, ILogger<CustomerGrpcService> log)
    : CustomerService.CustomerServiceBase
{
    public override async Task<CustomerReply> GetCustomer(GetCustomerRequest request, ServerCallContext context)
    {
        log.LogInformation("gRPC GetCustomer {Id}", request.Id);

        var customer = await db.Customers.FindAsync([request.Id], context.CancellationToken)
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Customer {request.Id} not found"));

        return new CustomerReply { Id = customer.Id, Name = customer.Name, Email = customer.Email };
    }
}
