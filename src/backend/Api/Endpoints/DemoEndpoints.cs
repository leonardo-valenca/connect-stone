using Application.Orders.SimulatePayment;
using Mediator;

namespace Api.Endpoints;

public static class DemoEndpoints
{
    public static IEndpointRouteBuilder MapDemoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/demo/simulate-payment/{orderId:guid}", async (Guid orderId, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new SimulatePaymentCommand(orderId), cancellationToken);
            return result.ToHttpResult();
        })
        .WithTags("Demo");

        return endpoints;
    }
}
