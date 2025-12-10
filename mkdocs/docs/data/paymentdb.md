# Payment Database

The `paymentdb` database stores payment confirmations and merchant service configurations.

## Schema

```mermaid
erDiagram
    merchant_service ||--o{ payment_confirmation : processes

    merchant_service {
        int id PK
        string name
        string type
    }

    payment_confirmation {
        uuid id PK
        int merchant_service_id FK
        string confirmation_code
        timestamp created_at
    }
```

## Tables

### `merchant_service`

Payment provider configurations (e.g., Stripe). Used to track which service processed a payment.

### `payment_confirmation`

Records of completed payments. Links to merchant service and stores the external confirmation code. Referenced by `plan_subscription` in productdb.
