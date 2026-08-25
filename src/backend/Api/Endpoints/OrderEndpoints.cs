using Application.Orders.CreateOrder;
using Application.Orders.GetOrders;
using Mediator;

namespace Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/orders").WithTags("Orders");

        group.MapPost("/", async (CreateOrderRequestDto request, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(
                new CreateOrderCommand(request.CustomerName, request.Description, request.AmountInCents),
                cancellationToken);
            return result.ToHttpResult();
        });

        group.MapGet("/", async (IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetOrdersQuery(), cancellationToken);
            return result.ToHttpResult();
        });

        return endpoints;
    }
}

public sealed record CreateOrderRequestDto(string CustomerName, string Description, int AmountInCents);
