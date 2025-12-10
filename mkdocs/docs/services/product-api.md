# ProductApi

ProductApi is a GraphQL subgraph that manages subscription plans, pricing tiers, and discount codes.

## Entities

| Entity | Description |
|--------|-------------|
| **Plan** | Subscription plan with name, description, features, icon |
| **PriceTier** | Pricing option for a plan (price, duration, status) |
| **PlanSubscription** | Client's subscription to a plan |
| **DiscountCode** | Promotional discount codes |

## GraphQL Operations

### Queries

| Query | Auth | Description |
|-------|------|-------------|
| `getPlan(id)` | Any | Fetch plan by ID |
| `getPlans(duration)` | Any | List plans, optionally filtered |
| `getPlanByForeignId(id)` | Admin | Lookup by Stripe product ID |
| `getPriceTier(id)` | Any | Get price tier |
| `getPriceTierByForeignId(id)` | Admin | Lookup by Stripe price ID |
| `getPlanSubscription(id)` | Any | Get subscription |
| `getPlanSubscriptions()` | Any | List subscriptions |
| `getDiscountCode(id)` | Admin | Get discount code |
| `getDiscountCodes()` | Admin | List discount codes |

### Mutations

| Mutation | Auth | Description |
|----------|------|-------------|
| `createPlan(input)` | Admin | Create plan with tiers and features |
| `updatePlan(input)` | Admin | Full update of plan |
| `patchPlan(input)` | Admin | Partial update |
| `deletePlan(id)` | Admin | Delete plan |
| `patchPriceTier(input)` | Admin | Update price tier |
| `deletePriceTier(id)` | Admin | Deactivate price tier |
| `subscribeToPlan(input)` | Client | Subscribe to a plan |
| `endSubscription(id)` | Client | End subscription |
| `createPlanSubscription(input)` | Admin | Create subscription for client |
| `createDiscountCode(input)` | Admin | Create discount code |
| `patchDiscountCode(input)` | Admin | Update discount code |
| `deleteDiscountCode(id)` | Admin | Delete discount code |

### Subscriptions (WebSocket)

- `onPlanUpdated(id)` - Real-time plan updates
- `onPriceTierUpdated(id)` - Real-time price tier updates

## Configuration

```ini
ConnectionStrings__ProductDb=Host=localhost;Database=productdb;...
Kafka__BrokerList=kafka:9092
REDIS_URL=redis:6379
Otlp__Endpoint=http://otel-collector:4317
```

## Kafka Events

Publishes to topics: `plans`, `priceTiers`, `planSubscriptions`, `discountCodes`, `notifications`

## Running

```powershell
cd dotnet/ProductApi
dotnet run
```

Default port: `5200`
