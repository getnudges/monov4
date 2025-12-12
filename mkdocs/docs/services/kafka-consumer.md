# KafkaConsumer

The **KafkaConsumer** service is a message-driven event processor that listens to Kafka topics and executes business logic based on events published by other services. It acts as a critical integration hub, handling domain events and translating them into downstream actions such as GraphQL API calls and external service integrations (Stripe, Twilio).

## Overview

The service runs as a CLI application that accepts a topic name as a command-line argument. Each topic has its own middleware pipeline and configuration, allowing independent scaling and deployment of consumers per topic.

```
dotnet run -- <topic-command>
```

Available commands:

| Command | Topic | Description |
|---------|-------|-------------|
| `notifications` | Notifications | SMS notification delivery |
| `plans` | Plans | Plan creation and updates |
| `price-tiers` | PriceTiers | Pricing tier management |
| `plan-subscriptions` | PlanSubscriptions | Subscription lifecycle events |
| `payments` | Payments | Payment confirmations |
| `clients` | Clients | Client creation and updates |
| `user-authentication` | UserAuthentication | User login/logout events |
| `stripe-webhooks` | StripeWebhooks | External (Stripe) events |

## Architecture

### Message Flow

```
Kafka Topic
    ↓
KafkaMessageProcessor
    ↓
TracingMiddleware (OpenTelemetry context)
    ↓
[Optional: Retry/CircuitBreaker Middleware]
    ↓
Domain-Specific Middleware
    ├→ Parse event type
    ├→ Execute handler logic
    ├→ Call external APIs (GraphQL, Stripe, Twilio)
    └→ Produce downstream events (optional)
```

### Project Structure

```
KafkaConsumer/
├── Program.cs                    # Entry point with CLI routing
├── HandlerBuilders.cs            # Service configuration per topic
├── MessageHandlerService.cs      # Hosted service running processor
├── Middleware/                   # Event handlers by message type
│   ├── ClientMessageMiddleware.cs
│   ├── NotificationMessageMiddleware.cs
│   ├── PaymentMessageMiddleware.cs
│   ├── PlanMessageMiddleware.cs
│   ├── PlanSubscriptionMiddleware.cs
│   ├── PriceTierMessageMiddleware.cs
│   ├── UserAuthenticationMessageMiddleware.cs
│   └── StripeWebhookMessageMiddleware.cs
├── Services/                     # Business logic and integrations
│   ├── StripeService.cs          # Stripe integration
│   ├── TwilioNotifier.cs         # SMS delivery (production)
│   └── LocalNotifier.cs          # SMS logging (development)
└── GraphQL/                      # Generated GraphQL client code
    └── Mutations/                # GraphQL mutation definitions
```

## Middleware Handlers

Each middleware handles specific event types from its topic:

### ClientMessageMiddleware

Handles client lifecycle events.

| Event | Action |
|-------|--------|
| `ClientCreatedEvent` | Creates Stripe customer, sends welcome SMS |
| `ClientUpdatedEvent` | Sends notification about updated data |

### NotificationMessageMiddleware

Handles SMS notification delivery.

| Event | Action |
|-------|--------|
| `SendSmsNotificationEvent` | Localizes text, sends via Twilio |

### PaymentMessageMiddleware

Handles payment confirmations.

| Event | Action |
|-------|--------|
| `PaymentCompletedEvent` | Creates plan subscription (dev mode) |

### PlanMessageMiddleware

Handles plan management with resilience policies (retry, circuit breaker).

| Event | Action |
|-------|--------|
| `PlanCreatedEvent` | Creates product in Stripe |
| `PlanUpdatedEvent` | Updates product in Stripe |

### PlanSubscriptionMiddleware

Handles subscription lifecycle.

| Event | Action |
|-------|--------|
| `PlanSubscriptionCreatedEvent` | Updates client, publishes notification |

### PriceTierMessageMiddleware

Handles pricing changes.

| Event | Action |
|-------|--------|
| `PriceTierCreatedEvent` | Creates price in Stripe |
| `PriceTierUpdatedEvent` | Updates pricing via GraphQL |
| `PriceTierDeletedEvent` | Removes price from Stripe |

### UserAuthenticationMessageMiddleware

Handles authentication events.

| Event | Action |
|-------|--------|
| `UserLoggedInEvent` | Sends login confirmation SMS |
| `UserLoggedOutEvent` | (Logged only) |

### StripeWebhookMessageMiddleware

Handles Stripe webhook events and synchronizes external products and pricing with local plans and price tiers.

| Event | Action |
|-------|--------|
| `StripeProductCreatedEvent` | Creates or patches plan from Stripe product |
| `StripeProductUpdatedEvent` | Updates plan with Stripe product changes |
| `StripeProductDeletedEvent` | Deletes plan when Stripe product is removed |
| `StripePriceCreatedEvent` | Creates price tier from Stripe price |
| `StripePriceUpdatedEvent` | Updates price tier pricing |
| `StripePriceDeletedEvent` | Deletes price tier when Stripe price is removed |
| `StripeCheckoutCompletedEvent` | Creates payment confirmation and plan subscription after checkout |

## Configuration

### Required Environment Variables

```ini
# Kafka
Kafka__BrokerList=kafka:29092

# GraphQL API
GRAPHQL_API_URL=http://graphql-gateway:5443/graphql

# Authentication
Oidc__Realm=nudges
Oidc__ServerUrl=https://auth.example.com
Oidc__ClientId=kafka-consumer
Oidc__ClientSecret=<secret>
AUTH_API_URL=https://auth-api.example.com

# Stripe
STRIPE_API_KEY=sk_test_...
STRIPE_API_URL=https://api.stripe.com

# Twilio (SMS)
TWILIO_ACCOUNT_SID=AC...
TWILIO_AUTH_TOKEN=<token>
TWILIO_MESSAGE_SERVICE_SID=MG...

# Localization
LOCALIZATION_API_URL=http://localization-api:5000

# Caching
REDIS_URL=redis:6379
WarpCache__Url=http://warpcache:5000

# Observability (optional)
Otlp__Endpoint=http://otel-collector:4317
```

## Dependencies

### Internal Projects

- `Nudges.Kafka` - Kafka infrastructure and message processor
- `Nudges.Kafka.Events` - Event type definitions
- `Nudges.Contracts` - Domain models
- `Nudges.Auth` / `Nudges.Auth.Web` - Authentication
- `Nudges.Telemetry` - OpenTelemetry integration
- `Nudges.Localization.Client` - Text localization
- `Nudges.Stripe` - Stripe utilities
- `Monads` - Result/Maybe error handling
- `Precision.WarpCache` - Distributed caching

### External Services

- **Kafka** - Message broker
- **GraphQL Gateway** - Internal API for data mutations
- **Stripe** - Payment and product management
- **Twilio** - SMS delivery
- **Localization API** - Text translation

## Local Development

### Running a Specific Consumer

```powershell
cd dotnet/KafkaConsumer
dotnet run -- clients
```

### Debug Profiles

The `launchSettings.json` contains profiles for each topic, allowing easy debugging in your IDE:

- `KafkaConsumer (Notifications)`
- `KafkaConsumer (Plans)`
- `KafkaConsumer (Clients)`
- etc.

### Development vs Production Mode

In development, SMS notifications are logged to the console instead of being sent via Twilio. This is controlled by the `LocalNotifier` vs `TwilioNotifier` service registration.

## Error Handling

The service uses several error handling strategies:

1. **Result Monads** - Business logic returns `Result<T, Exception>` for type-safe error handling
2. **Retry Policies** - Polly-based retries for transient failures (e.g., Stripe API)
3. **Circuit Breaker** - Prevents cascading failures when external services are down
4. **Dead Letter Queue** - Unprocessable messages can be routed to a DLQ topic

## Observability

- **OpenTelemetry Tracing** - Distributed traces across service boundaries
- **Structured Logging** - Contextual logs with message IDs and correlation
- **Prometheus Metrics** - Exposed via Kestrel endpoint for scraping
