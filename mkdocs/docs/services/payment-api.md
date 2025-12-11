# PaymentApi

PaymentApi is a GraphQL subgraph that manages checkout sessions and payment confirmations via Stripe integration.

## Entities

| Entity | Description |
|--------|-------------|
| **MerchantService** | Payment provider configuration |
| **PaymentConfirmation** | Record of completed payment |
| **CheckoutSession** | Stripe checkout session info |

## GraphQL Operations

### Queries

| Query | Auth | Description |
|-------|------|-------------|
| `getMerchantService(id)` | Admin | Get merchant service |
| `getMerchantServices()` | Admin | List merchant services |
| `getPaymentConfirmation(id)` | Admin | Get payment confirmation |
| `getPaymentConfirmationByCode(code)` | Admin | Lookup by confirmation code |

### Mutations

| Mutation | Auth | Description |
|----------|------|-------------|
| `createCheckoutSession(input)` | Client | Create Stripe checkout session |
| `cancelCheckoutSession(input)` | Client | Cancel checkout session |
| `createPaymentConfirmation(input)` | Admin | Record payment confirmation |

### createCheckoutSession Input

```graphql
input CreateCheckoutSessionInput {
  customerId: String!
  priceForeignServiceId: String!  # Stripe price ID
  successUrl: String!
  cancelUrl: String!
}
```

Returns checkout URL for redirect to Stripe.

## Configuration

```ini
ConnectionStrings__PaymentDb=Host=localhost;Database=paymentdb;...
STRIPE_API_KEY=sk_...
STRIPE_API_URL=https://api.stripe.com
Kafka__BrokerList=kafka:9092
REDIS_URL=redis:6379
Otlp__Endpoint=http://otel-collector:4317
```

## Kafka Events

Publishes to topics: `payments`, `notifications`

## Running

```powershell
cd dotnet/PaymentApi
dotnet run
```

Default port: `5400`
