# Plan: Replace GraphQL Calls in Webhooks with Kafka Events

## Overview

The Webhooks service currently makes synchronous GraphQL API calls to query and mutate data. This creates tight coupling and potential latency issues. We'll replace these with Kafka events, making the service fully event-driven.

## Current State Analysis

### Active GraphQL Operations in Webhooks

**Stripe Webhook Handlers (6 operations):**

| Handler | GraphQL Calls | Purpose |
|---------|---------------|---------|
| `CheckoutSessionCompletedCommand` | `GetClientByCustomerId` + `CreatePaymentConfirmation` + `CreatePlanSubscription` | Create subscription after payment |
| `ProductCreatedCommand` | `GetPlanByForeignId` | Check if plan exists |
| `ProductUpdatedCommand` | `GetPlanByForeignId` + `PatchPlan` | Update plan from Stripe |
| `ProductDeletedCommand` | `GetPlanByForeignId` + `DeletePlan` | Delete plan |
| `PriceCreatedCommand` | `GetPlanByForeignId` + `PatchPriceTier` | Link Stripe price to tier |
| `PriceUpdatedCommand` | `GetPriceTierByForeignId` + `PatchPriceTier` | Update price tier |
| `PriceDeletedCommand` | `GetPriceTierByForeignId` + `DeletePriceTier` | Delete price tier |

**Twilio Webhook Handlers (2 queries):**

| Handler | GraphQL Calls | Purpose |
|---------|---------------|---------|
| `AnnouncementCommand` | `GetClientByPhoneNumber` | Verify client has active subscription |
| Various SMS handlers | `SmsLocaleLookup` | Get user's locale preference |

## Proposed Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         CURRENT FLOW                                 │
├─────────────────────────────────────────────────────────────────────┤
│  Stripe ──webhook──> Webhooks ──GraphQL──> ProductApi ──> DB        │
│                          │                                           │
│                          └──GraphQL──> UserApi ──> DB               │
└─────────────────────────────────────────────────────────────────────┘

                              ↓ becomes ↓

┌─────────────────────────────────────────────────────────────────────┐
│                          NEW FLOW                                    │
├─────────────────────────────────────────────────────────────────────┤
│  Stripe ──webhook──> Webhooks ──Kafka──> KafkaConsumer ──> APIs     │
│                          │                                           │
│                          └──Cache──> WarpCache (for lookups)        │
└─────────────────────────────────────────────────────────────────────┘
```

## Implementation Plan

### Phase 1: New Kafka Events for Stripe Webhooks

Create new events in `Nudges.Kafka.Events/StripeWebhooks.cs`:

```csharp
// Topic: "stripe-webhooks"
[EventModel(typeof(StripeWebhookKey))]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(StripeCheckoutCompletedEvent), "stripe.checkoutCompleted")]
[JsonDerivedType(typeof(StripeProductCreatedEvent), "stripe.productCreated")]
[JsonDerivedType(typeof(StripeProductUpdatedEvent), "stripe.productUpdated")]
[JsonDerivedType(typeof(StripeProductDeletedEvent), "stripe.productDeleted")]
[JsonDerivedType(typeof(StripePriceCreatedEvent), "stripe.priceCreated")]
[JsonDerivedType(typeof(StripePriceUpdatedEvent), "stripe.priceUpdated")]
[JsonDerivedType(typeof(StripePriceDeletedEvent), "stripe.priceDeleted")]
public abstract partial record StripeWebhookEvent;
```

**Event Definitions:**

| Event | Properties |
|-------|------------|
| `StripeCheckoutCompletedEvent` | `CustomerId`, `InvoiceId`, `PriceId`, `SessionId` |
| `StripeProductCreatedEvent` | `ProductId`, `Name`, `Description`, `IconUrl`, `Active`, `Metadata` |
| `StripeProductUpdatedEvent` | `ProductId`, `Name`, `Description`, `IconUrl`, `Active`, `Metadata` |
| `StripeProductDeletedEvent` | `ProductId` |
| `StripePriceCreatedEvent` | `PriceId`, `ProductId`, `Amount`, `Currency`, `Nickname` |
| `StripePriceUpdatedEvent` | `PriceId`, `Amount`, `Nickname` |
| `StripePriceDeletedEvent` | `PriceId` |

### Phase 2: New KafkaConsumer Middleware

Create `StripeWebhookMessageMiddleware.cs` that:

1. Handles `StripeProductCreatedEvent`:
   - Publishes `ForeignProductSynchronizedEvent` (existing event)

2. Handles `StripeProductUpdatedEvent`:
   - Calls `PatchPlan` mutation via GraphQL

3. Handles `StripeProductDeletedEvent`:
   - Calls `DeletePlan` mutation via GraphQL

4. Handles `StripePriceCreatedEvent`:
   - Calls `PatchPriceTier` mutation via GraphQL

5. Handles `StripePriceUpdatedEvent`:
   - Calls `PatchPriceTier` mutation via GraphQL

6. Handles `StripePriceDeletedEvent`:
   - Calls `DeletePriceTier` mutation via GraphQL

7. Handles `StripeCheckoutCompletedEvent`:
   - Calls `CreatePaymentConfirmation` + `CreatePlanSubscription` mutations

### Phase 3: Lookup Data via Cache (for Twilio handlers)

For `SmsLocaleLookup` and `GetClientByPhoneNumber`, we have two options:

**Option A: Keep GraphQL for reads (Recommended for now)**
- These are read-only queries
- Low latency requirement for SMS responses
- Event sourcing for reads adds complexity

**Option B: Cache-based lookups**
- Populate WarpCache from Kafka events
- `UserLoggedInEvent` → cache locale by phone
- `ClientCreatedEvent` → cache client data by phone
- Query cache instead of GraphQL

**Recommendation:** Keep GraphQL for Twilio reads initially. Focus on eliminating write operations first.

### Phase 4: Refactor Webhooks Service

1. **Remove GraphQL mutations** - Replace with Kafka event publishing
2. **Remove GraphQL queries for Stripe** - Not needed, just publish events
3. **Keep GraphQL queries for Twilio** - Read operations for locale/client lookup
4. **Simplify command handlers** - Just parse webhook and publish event

**Before (ProductCreatedCommand):**
```csharp
var plan = await client.GetPlanByForeignId(productId);
if (plan != null) return; // Already exists
await kafkaProducer.Produce(ForeignProductSynchronizedEvent);
```

**After (ProductCreatedCommand):**
```csharp
await kafkaProducer.Produce(StripeProductCreatedEvent.FromStripeProduct(product));
// KafkaConsumer handles the rest
```

### Phase 5: Update Topic Configuration

Add to `Topics.cs`:
```csharp
public const string StripeWebhooks = "stripe-webhooks";
```

Add to `docker-compose.yml` kafka-init-topics:
```bash
kafka-topics --create --topic stripe-webhooks --partitions 2 --replication-factor 1
```

## Files to Modify

### New Files
- [ ] `dotnet/Nudges.Kafka.Events/StripeWebhooks.cs` - Event definitions
- [ ] `dotnet/KafkaConsumer/Middleware/StripeWebhookMessageMiddleware.cs` - Event handler

### Modified Files
- [ ] `dotnet/Nudges.Kafka.Events/Topics.cs` - Add new topic constant
- [ ] `dotnet/KafkaConsumer/Program.cs` - Add CLI command for stripe-webhooks consumer
- [ ] `dotnet/KafkaConsumer/HandlerBuilders.cs` - Configure new handler
- [ ] `dotnet/Nudges.Webhooks/StripeHandlers/*.cs` - Replace GraphQL with Kafka publishing
- [ ] `docker-compose.yml` - Add topic creation, add consumer service

### Files to Delete (after migration)
- [ ] `dotnet/Nudges.Webhooks/GraphQL/Mutations/` - All mutation files
- [ ] `dotnet/Nudges.Webhooks/GraphQL/Queries/GetPlanByForeignId.graphql`
- [ ] `dotnet/Nudges.Webhooks/GraphQL/Queries/GetPriceTierByForeignId.graphql`

### Files to Keep
- [ ] `dotnet/Nudges.Webhooks/GraphQL/Queries/SmsLocaleLookup.graphql` - Twilio needs this
- [ ] `dotnet/Nudges.Webhooks/GraphQL/Queries/GetClientByPhoneNumber.graphql` - Twilio needs this

## Migration Steps

1. **Create events** - Define all Stripe webhook events
2. **Create consumer** - Implement StripeWebhookMessageMiddleware
3. **Register consumer** - Add to Program.cs and HandlerBuilders.cs
4. **Add topic** - Update docker-compose.yml
5. **Update Webhooks** - One handler at a time:
   - Start with `ProductCreatedCommand` (simplest)
   - Then `ProductUpdatedCommand`, `ProductDeletedCommand`
   - Then price handlers
   - Finally `CheckoutSessionCompletedCommand` (most complex)
6. **Test each handler** - Verify end-to-end flow
7. **Remove dead code** - Delete unused GraphQL files

## Rollback Strategy

Keep the GraphQL client in Webhooks until fully validated. Use a feature flag or environment variable to switch between:
- `WEBHOOKS_USE_KAFKA=true` → New Kafka-based flow
- `WEBHOOKS_USE_KAFKA=false` → Original GraphQL flow

## Questions to Consider

1. **Idempotency**: How do we handle duplicate webhook deliveries?
   - Answer: Stripe webhook IDs are unique, use as deduplication key

2. **Ordering**: Does event order matter for product/price?
   - Answer: Yes for create→update, use same partition key (product ID)

3. **Error handling**: What if KafkaConsumer fails to process?
   - Answer: Existing retry + circuit breaker + DLQ pattern handles this

4. **Latency**: Checkout flow needs to be reasonably fast
   - Answer: Kafka is fast enough, customer already has Stripe confirmation
