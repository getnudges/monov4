# Product Database

The `productdb` database stores subscription plans, pricing, and related entities.

## Schema

```mermaid
erDiagram
    plan ||--o| plan_features : has
    plan ||--o{ price_tier : contains
    price_tier ||--o{ plan_subscription : "subscribed via"
    price_tier ||--o{ discount_code : "applies to"
    price_tier ||--o{ trial_offer : "offers"
    discount_code ||--o{ discount : creates
    plan_subscription ||--o{ discount : "applied to"
    plan_subscription ||--o{ trial : starts
    trial_offer ||--o{ trial : creates

    plan {
        uuid id PK
        string name
        string description
        boolean is_active
        string icon_url
        string foreign_service_id
    }

    plan_features {
        uuid plan_id PK,FK
        boolean ai_support
        int max_messages
        int support_tier
    }

    price_tier {
        uuid id PK
        uuid plan_id FK
        string name
        decimal price
        interval duration
        string status
        string foreign_service_id
    }

    plan_subscription {
        uuid id PK
        uuid price_tier_id FK
        uuid client_id
        uuid payment_confirmation_id
        timestamp start_date
        timestamp end_date
        string status
    }

    discount_code {
        uuid id PK
        uuid price_tier_id FK
        string code
        string name
        decimal discount
        interval duration
        timestamp expiry_date
    }

    discount {
        uuid id PK
        uuid discount_code_id FK
        uuid plan_subscription_id FK
    }

    trial_offer {
        uuid id PK
        uuid price_tier_id FK
    }

    trial {
        uuid id PK
        uuid plan_subscription_id FK
        uuid trial_offer_id FK
    }
```

## Tables

### `plan`

Base entity for subscription products. Each plan has features and one or more price tiers.

### `plan_features`

Features included with a plan (AI support, message limits, support tier). One-to-one with plan.

### `price_tier`

Pricing options for a plan. A plan may have multiple tiers (e.g., monthly, yearly) with different prices and durations.

### `plan_subscription`

Records a client's subscription to a specific price tier, with start/end dates and payment confirmation reference.

### `discount_code`

Promotional codes that can be applied to subscriptions for a discount percentage and duration.

### `trial_offer` / `trial`

Trial period offerings and active trials for subscriptions.
