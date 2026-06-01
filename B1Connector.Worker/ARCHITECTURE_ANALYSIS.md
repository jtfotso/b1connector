# B1Connector — Architecture Analysis

## Current Architecture

The project follows a **Monolithic ASP.NET Core application** with the following structure:

```
B1Connector.Worker/
├── Program.cs                      # DI, pipeline, endpoints
├── Models/                         # Domain entities (Tenant, SyncJob, SyncLog)
├── Data/AppDbContext.cs            # EF Core + SQL Server
├── Jobs/                           # BackgroundService worker + DB-backed queue
├── Connectors/Shopify/             # Webhook handlers, mapper, DTOs
├── SapB1/                          # ISapServiceLayerClient + impl + mock
├── Infrastructure/                 # EncryptionService, TenantService
└── Dashboard/                      # Blazor Server UI (Pages, Services)
```

**Data flow:** Shopify webhook → webhook handler → DB queue (SyncJobs table) → `SyncJobWorker` polls every 30s → SAP B1 Service Layer → mark complete.

---

## Issues with the Current Architecture

| # | Issue | Severity |
|---|-------|----------|
| 1 | **Single project monolith** — domain, infrastructure, web, and UI all in one assembly. Won't scale as connectors grow. | Medium |
| 2 | **Polling-based worker** — 30s poll on DB is wasteful and adds latency. No in-memory signaling. | Medium |
| 3 | **No connector abstraction** — `ConnectorType` enum with a `switch` in the worker. Adding a connector means modifying the worker (violates Open/Closed Principle). | High |
| 4 | **Static webhook handlers** — `ShopifyWebhookHandler` and `ShopifyInventoryHandler` are `static` classes. Hard to unit-test, can't use DI properly. | Medium |
| 5 | **Duplicated HMAC/tenant logic** — both Shopify handlers copy-paste the same `ReadBodyAsync`, `IsValidShopifyRequest`, tenant lookup. ~50 lines duplicated. | Medium |
| 6 | **SyncJobWorker does too much** — tenant resolution, SAP client creation, job dispatch, Shopify deserialization all in one class. Violates Single Responsibility Principle (SRP). | High |
| 7 | **Inconsistent DI** — mock SAP client comes from DI, real one is manually `new`'d up with a raw `HttpClient` (no resilience policies). | High |
| 8 | **Direct DB access from Blazor** — dashboard services query `AppDbContext` directly. No read model or CQRS separation. | Low |
| 9 | **No unit tests** — no test project exists in the solution. | Low |

---

## Recommended Architecture

For this project's current scope, a **Clean Architecture** approach split across a few assemblies is recommended:

```
B1Connector.sln
├── B1Connector.Domain/              # Pure C# — zero dependencies
│   ├── Models/                      # Tenant, SyncJob, SyncLog (entities)
│   ├── Enums/                       # ConnectorType, SyncJobStatus
│   └── Interfaces/                  # IConnectorHandler, ISapClient
│
├── B1Connector.Application/         # Use cases — depends on Domain only
│   ├── Connectors/                  # IConnectorHandler, ConnectorRegistry
│   │   ├── Shopify/                 # ShopifyConnectorHandler (encapsulates all Shopify logic)
│   │   └── Common/                  # Shared HMAC validator, body reader
│   ├── Jobs/                        # Enqueue + dispatch logic
│   └── Services/                    # Application services
│
├── B1Connector.Infrastructure/      # External concerns — depends on Application
│   ├── Persistence/                 # AppDbContext, EF Core config
│   ├── SapB1/                       # ServiceLayerClient (real SAP HTTP client)
│   └── Services/                    # EncryptionService
│
├── B1Connector.Worker/              # Entry point + presentation
│   ├── Program.cs                   # Minimal API host
│   ├── WebhookEndpoints/            # Per-connector webhook endpoints
│   └── Dashboard/                   # Blazor Server UI
│
└── B1Connector.Tests/               # Unit + integration tests
```

### Key Architectural Improvements

1. **Connector Abstraction (`IConnectorHandler`):** Each connector implements `IConnectorHandler` and registers itself in a `ConnectorRegistry`. The worker resolves the correct handler by `ConnectorType` without a switch statement — enabling Open/Closed Principle compliance.

2. **Webhook Middleware Pipeline:** Instead of static classes with duplicated logic, extract a shared webhook validation middleware that handles HMAC verification, tenant resolution, and body reading. Each connector only supplies its HMAC secret and payload format.

3. **Channel-based Signaling:** Replace the 30s DB poll with a `System.Threading.Channels.Channel<SyncJob>` — webhook handlers write to the channel, the worker reads from it. Instant processing with zero polling overhead.

4. **SAP Client Factory:** Resolve `ISapServiceLayerClient` through DI, making the mock/real switch transparent to consumers. Register a factory that builds the client per-tenant with proper `HttpClient` resilience policies (retry, circuit breaker).

---

## Is Clean Architecture Worth It?

**Yes, for this project.** The connector pattern is already growing (the `ConnectorType` enum has 6 planned types). Without proper abstractions, every new connector will increase the complexity of the worker's switch statement and scatter logic. Clean Architecture would also make the code testable in isolation — critical when integrating with external systems like SAP and Shopify.

The investment is moderate (~1-2 days of refactoring) but pays off immediately on the next connector addition.
