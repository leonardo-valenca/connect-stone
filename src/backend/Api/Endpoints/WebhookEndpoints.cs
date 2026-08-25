using Application.Orders.HandleWebhook;
using ConnectStone.Sdk.Webhooks;
using Mediator;

namespace Api.Endpoints;

public static class WebhookEndpoints
{
    public static IEndpointRouteBuilder MapWebhookEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/webhooks/pagarme", async (
            HttpRequest request,
            IWebhookAuthenticator authenticator,
            IWebhookEventParser parser,
            IMediator mediator,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            if (!authenticator.IsAuthentic(request.Headers.Authorization))
            {
                return Results.Unauthorized();
            }

            using var reader = new StreamReader(request.Body);
            var rawBody = await reader.ReadToEndAsync(cancellationToken);

            var webhookEvent = parser.Parse(rawBody);
            switch (webhookEvent)
            {
                case ChargePaidEvent paid:
                    await mediator.Send(
                        new HandleWebhookCommand(paid.Data.Order.Id, WebhookOutcome.Paid, paid.Data.PaidAt ?? paid.CreatedAt),
                        cancellationToken);
                    break;

                case ChargeRefundedEvent refunded:
                    await mediator.Send(
                        new HandleWebhookCommand(refunded.Data.Order.Id, WebhookOutcome.Refunded, refunded.CreatedAt),
                        cancellationToken);
                    break;

                default:
                    // Unrecognized event type, acknowledge without processing (see IWebhookEventParser).
                    logger.LogInformation("Ignored webhook payload with no recognized event type.");
                    break;
            }

            return Results.Ok();
        })
        .WithTags("Webhooks");

        return endpoints;
    }
}
