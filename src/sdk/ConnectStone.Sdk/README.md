# ConnectStone.Sdk

A typed .NET client for Stone's **Connect Stone** integration: creating payment orders that appear
on a physical Stone card machine, plus a webhook handler for the Pagar.me payment-confirmation
events that follow.

> Connect Stone is not a separate API: it's Pagar.me's Core v5 API (`api.pagar.me/core/v5`) used
> with POS-specific fields. This package wraps that API with typed requests/responses, resilience
> for the calls that are safe to retry, and typed webhook events.

Pagar.me does publish an official .NET client
([`pagarme/pagarme-net-standard-sdk`](https://github.com/pagarme/pagarme-net-standard-sdk)), an
auto-generated wrapper covering the general payments API (charges, customers, subscriptions,
orders). It has no webhook parsing or authentication and no POS-specific ergonomics for
`poi_payment_settings`, so it doesn't cover the Connect Stone side of this at all. This package
exists to fill that gap specifically.

## Install

```bash
dotnet add package ConnectStone.Sdk
```

*(Not yet published to nuget.org, see the repo root README for how to reference it as a project
during development.)*

## Register the client

```csharp
builder.Services.AddConnectStoneClient(options =>
{
    options.SecretKey = builder.Configuration["ConnectStone:SecretKey"]!;
    // Optional, see the caveat below. Leave it null if you don't have one.
    options.ServiceRefererName = builder.Configuration["ConnectStone:ServiceRefererName"];
});

// Or bind directly from an "ConnectStone" configuration section:
builder.Services.AddConnectStoneClient(builder.Configuration);
```

## Create an order

```csharp
var order = await connectStoneClient.CreateOrderAsync(new CreateOrderRequest(
    Customer: new CustomerRequest("Jane Doe", "jane@example.com"),
    Items: [new OrderItemRequest(Amount: 1500, Description: "Coffee", Quantity: 1)],
    Closed: false, // false = show on the linked card machine
    PoiPaymentSettings: new PoiPaymentSettings(
        Type: PaymentType.Credit,
        Installments: 1,
        InstallmentType: InstallmentType.Merchant,
        Visible: true,
        DisplayName: "Coffee shop",
        PrintOrderReceipt: true)));
```

## Close an order once payment is confirmed

Close the order after your webhook handler confirms the outcome. Leaving paid/failed orders open
can make the POS misbehave, and Pagar.me caps integrations at **30 simultaneously open orders**
(`TooManyOpenOrdersException` is thrown if you hit that cap, see the caveat below).

```csharp
await connectStoneClient.CloseOrderAsync(orderId, OrderCloseStatus.Paid);
```

## Handle webhooks

Webhooks are registered manually in the Pagar.me dashboard (Account > Configurações > Webhooks),
not via this API. Pagar.me secures them with Basic/OAuth2 credentials on the endpoint itself, not
HMAC payload signing, and those credentials are optional on Pagar.me's side, so this SDK treats
them as optional too: configure them to match whatever you set up on the Pagar.me webhook (or leave
both blank to accept unauthenticated webhooks, see the security note on `WebhookAuthenticator`).

```csharp
builder.Services.AddConnectStoneWebhooks(options =>
{
    options.Username = builder.Configuration["ConnectStone:Webhook:Username"];
    options.Password = builder.Configuration["ConnectStone:Webhook:Password"];
});
```

```csharp
app.MapPost("/webhooks/pagarme", async (
    HttpRequest request,
    IWebhookAuthenticator authenticator,
    IWebhookEventParser parser,
    IConnectStoneClient connectStoneClient) =>
{
    if (!authenticator.IsAuthentic(request.Headers.Authorization))
    {
        return Results.Unauthorized();
    }

    using var reader = new StreamReader(request.Body);
    var rawBody = await reader.ReadToEndAsync();
    var webhookEvent = parser.Parse(rawBody);

    switch (webhookEvent)
    {
        case ChargePaidEvent paid:
            await connectStoneClient.CloseOrderAsync(paid.Data.Order.Id, OrderCloseStatus.Paid);
            break;
        case ChargeRefundedEvent refunded:
            await connectStoneClient.CloseOrderAsync(refunded.Data.Order.Id, OrderCloseStatus.Canceled);
            break;
        // null (unrecognized event type) is intentionally ignored, not an error.
        // Pagar.me may add new event types over time.
    }

    return Results.Ok();
});
```

## Known caveats

- **No sandbox for POS reflection**: Pagar.me states orders created against a test/sandbox key are
  not reflected on the physical machine. Realistic end-to-end testing requires production keys and
  real hardware.
- **The webhook payload doesn't match the published docs**: verified against a real `charge.paid`
  delivery, the charge's own fields (`id`, `amount`, `status`, ...) sit directly on a top-level
  `data` object alongside `customer`, `order`, and `last_transaction`, not under a separate nested
  `charge` object, and the transaction is `last_transaction`, not `transaction`. `WebhookChargeData`
  is modeled on the real payload, not the docs.
- **`ServiceRefererName` is documented as required but treated as optional here**: Connect Stone's
  docs describe it as a unique id issued through the Stone Partner Program (via Stone's
  Integrations team or a Bizdev contact), not something self-service. A working reference
  integration sends requests without this header at all, so `ConnectStoneClientOptions` makes it
  nullable and the header is simply omitted when it's not set, rather than requiring a value that
  may not be obtainable.
- **Order creation is never auto-retried**: Pagar.me doesn't document an idempotency key for
  `POST /orders`, so this SDK does not retry it. A retried POST after a lost response could create
  a duplicate order on the machine. `GetOrderAsync`/`CloseOrderAsync` (idempotent) are retried on
  5xx/429.
- **`TooManyOpenOrdersException` detection is best-effort**: the docs state the 30-open-order cap
  exists but don't publish the exact error response shape, so detection is a text-based heuristic.
  If your account's real error response doesn't match, `ConnectStoneApiException` is still thrown
  with the raw body attached, adjust the heuristic in `ConnectStoneClient` if needed.
- **Cancelling a charge is a different operation from closing an order**: `CancelChargeAsync` calls
  `DELETE /core/v5/charges/{id}` (full or partial refund, triggers a `charge.refunded` webhook).
  It is not the same as `CloseOrderAsync(id, OrderCloseStatus.Canceled)`, which just removes an
  order from the POS queue.
- **Webhook authentication is optional, and running without it is a real tradeoff**: if you leave
  `WebhookAuthenticatorOptions.Username`/`Password` unset (matching a Pagar.me webhook with no
  auth configured), `IsAuthentic` accepts every request. Fine for local testing; for anything
  reachable from the internet, anyone who finds the URL could POST a fake `charge.paid` and get an
  order marked paid without a real charge happening. Set credentials for production use.
