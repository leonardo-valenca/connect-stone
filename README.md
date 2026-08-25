# Connect Stone

Payment integrations that mishandle retries, webhooks, or edge cases cost real money: duplicate
charges, orders stuck in limbo, reconciliation headaches for finance. This is a reusable .NET SDK
for Stone's **Connect Stone** integration: creating payment orders that appear on a physical Stone
card machine, and closing them once Pagar.me confirms payment. Plus a demo app (API + Angular
dashboard) that proves the SDK actually works.

This is a rebuild, from scratch and generically, of a payment-machine integration originally built
for an employer. No proprietary code here, everything was re-derived from
[Connect Stone's public docs](https://connect-stone.stone.com.br/docs/o-que-é-a-api-connect-20).

## The three pieces

| Path | What it is | Who it's for |
|---|---|---|
| [`src/sdk/ConnectStone.Sdk`](src/sdk/ConnectStone.Sdk) | A typed .NET client + webhook handler for the Connect Stone/Pagar.me API | Other developers building their own integration |
| [`src/backend`](src/backend) | A Clean Architecture API that consumes the SDK | Proves the SDK works in a real app |
| [`src/frontend`](src/frontend) | An Angular dashboard with live order-status updates | Makes the flow visible to non-technical viewers |

Pagar.me does publish an official .NET client
([`pagarme/pagarme-net-standard-sdk`](https://github.com/pagarme/pagarme-net-standard-sdk)), an
auto-generated wrapper covering the general payments API (charges, customers, subscriptions,
orders). It has no webhook parsing or authentication, no POS-specific ergonomics for
`poi_payment_settings`, and no resilience tuned to what's actually safe to retry, so it doesn't
cover the Connect Stone side of this at all. This SDK fills that gap deliberately: typed
request/response models instead of loose JSON, resilience applied only to the calls that are
actually safe to retry (see
[caveats](src/sdk/ConnectStone.Sdk/README.md#known-caveats), retrying order creation blindly can
double-create an order on the machine), webhook authenticity checking, and a domain-level exception
for the documented 30-open-order cap instead of a raw 4xx.

## Architecture

```
                          ┌─────────────────────┐
   Browser  ──────────────▶   Angular dashboard   │
                          └──────────┬───────────┘
                                     │ REST + SignalR
                          ┌──────────▼───────────┐
                          │   Api (Minimal APIs)  │
                          ├───────────────────────┤
                          │      Application      │  Mediator commands, FluentValidation
                          ├───────────────────────┤
                          │        Domain         │  DemoOrder, zero external deps
                          ├───────────────────────┤
                          │     Infrastructure     │  EF Core/SQLite, SignalR hub,
                          │  (implements the SDK's │  ConnectStoneGateway
                          │   ports for this app)  │
                          └──────────┬───────────┘
                                     │ references
                          ┌──────────▼───────────┐
                          │   ConnectStone.Sdk    │  the reusable, standalone package
                          └──────────┬───────────┘
                                     │ HTTPS
                          ┌──────────▼───────────┐
                          │  Pagar.me Core v5 API  │──▶ physical Stone card machine
                          │   (= "Connect Stone")  │◀── charge.paid / charge.refunded webhook
                          └───────────────────────┘
```

`Infrastructure` depends on the SDK the same way any external consumer would. This repo doesn't
get special access to undocumented SDK internals.

## Real mode vs. simulate mode

Pagar.me has **no sandbox that reflects to the physical machine**, so realistic end-to-end testing
needs production API keys and real hardware. Most people looking at this repo won't have a Stone
machine, so the demo supports both:

- **Real mode**: create an order with your real Pagar.me keys, tap a real card on a real machine,
  the real `charge.paid` webhook lands on `/webhooks/pagarme` and closes the order.
- **Simulate mode**: click "Simulate payment" in the dashboard. This calls
  `POST /demo/simulate-payment/{orderId}`, which runs through the **exact same**
  `HandleWebhookCommand` code path a real webhook would, see
  [`SimulatePaymentCommandHandler`](src/backend/Application/Orders/SimulatePayment/SimulatePaymentCommandHandler.cs).
  It's not a fake shortcut around the real logic.

Both modes still call the **real** Pagar.me order-creation API. That part doesn't require
hardware, only the physical card-tap does. So even simulate mode needs a real (free) Pagar.me
account and API keys; there's no fully-offline mode, because the point is to demonstrate the actual
integration, not a mock of it.

`ServiceRefererName` is documented as required, but it's only issued through the Stone Partner
Program, and a working reference integration sends requests without it. So this SDK treats it as
optional: leave it unset and the header is simply omitted rather than sent blank.

## Running it

### Docker Compose (closest to production)

```bash
cp .env.example .env   # fill in your Pagar.me keys, see .env.example
docker compose up --build
```

Open http://localhost:8080. The `proxy` container (Caddy) serves the Angular build and reverse-proxies
`/orders`, `/webhooks`, `/demo`, and the `/hubs/order-status` SignalR endpoint to the API. One
origin, no CORS. The API has no published port of its own.

### Local development

```bash
# Backend
dotnet user-secrets set "ConnectStone:SecretKey" "sk_..." --project src/backend/Api
# ServiceRefererName and the webhook credentials below are all optional, see .env.example.
# Only set them if you actually have values for them.
dotnet user-secrets set "ConnectStone:ServiceRefererName" "..." --project src/backend/Api
dotnet user-secrets set "ConnectStone:Webhook:Username" "..." --project src/backend/Api
dotnet user-secrets set "ConnectStone:Webhook:Password" "..." --project src/backend/Api
dotnet run --project src/backend/Api   # http://localhost:5177, Scalar docs at /scalar/v1

# Frontend (separate terminal)
cd src/frontend
npm install
npm start   # http://localhost:4200, proxies API calls to :5177 (see proxy.conf.json)
```

To receive **real** webhooks locally, Pagar.me needs a public URL to call. Tunnel the API with
something like `ngrok http 5177` and point the Pagar.me dashboard's webhook config at
`https://<tunnel>/webhooks/pagarme`. Simulate mode doesn't need this.

## Repo structure

```
connect-stone/
  src/
    sdk/
      ConnectStone.Sdk/            # the reusable package, see its own README
      ConnectStone.Sdk.Tests/
    backend/
      Api/                         # Minimal APIs, SignalR hub mapping, Program.cs
      Application/                 # Mediator commands/queries, validation, ports
      Domain/                      # DemoOrder entity, zero external dependencies
      Infrastructure/              # EF Core+SQLite, SDK-backed gateway, SignalR hub
      tests/
        Domain.Tests/
        Application.Tests/
        Api.IntegrationTests/      # WebApplicationFactory, fakes the SDK gateway (no live calls)
    frontend/                      # Angular, standalone components + signals, Tailwind
  proxy/                           # Caddyfile + Dockerfile, reverse proxy + static SPA host
  docker-compose.yml
```

## Testing

```bash
dotnet test ConnectStone.slnx     # SDK + backend, 57 tests, no live credentials needed
cd src/frontend && npm test        # Vitest
```

The SDK's tests run against a stubbed `HttpMessageHandler` and the API's integration tests swap in
a fake `IConnectStoneGateway`, none of the automated test suite makes a real network call to
Pagar.me. The "does it really work" proof is the manual real-mode run above.

## Publishing the SDK

The package is fully pack-able (`dotnet pack src/sdk/ConnectStone.Sdk`) but **not yet published to
nuget.org**, deliberately, until the API surface has had time to settle.
[`.github/workflows/publish-sdk.yml`](.github/workflows/publish-sdk.yml) is wired to publish on a
`sdk-v*` tag push, but needs a `NUGET_API_KEY` repository secret added before it'll actually
succeed.

## License

[MIT](LICENSE)
