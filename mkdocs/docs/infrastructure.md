# Infrastructure Overview

Nudges is built as an event-driven microservices platform. This page provides a high-level view of how the components fit together.

## System Architecture

```mermaid
flowchart TB
    subgraph Internet
        Users[Users/Browsers]
        StripeExt[Stripe]
        TwilioExt[Twilio]
    end

    subgraph AWS["AWS Cloud"]
        subgraph Public["Public Subnet"]
            ALB[Application Load Balancer]
        end

        subgraph Private["Private Subnet"]
            subgraph ECS["ECS Cluster"]
                subgraph Frontend
                    AdminUI[Admin UI]
                    SignupUI[Signup UI]
                end

                subgraph Gateway
                    GQLGateway[GraphQL Gateway]
                end

                subgraph CoreAPIs["Core APIs"]
                    AuthAPI[AuthApi]
                    UserAPI[UserApi]
                    ProductAPI[ProductApi]
                    PaymentAPI[PaymentApi]
                end

                subgraph Workers["Background Workers"]
                    KConsumers[Kafka Consumers]
                    Webhooks[Webhooks Service]
                end

                subgraph Support["Support Services"]
                    WarpCache[WarpCache]
                    GraphMon[GraphMonitor]
                    LocalAPI[LocalizationApi]
                end
            end

            subgraph Data["Data Layer"]
                RDS[(PostgreSQL RDS)]
                ElastiCache[(ElastiCache Redis)]
                MSK[Amazon MSK / Kafka]
            end

            Keycloak[Keycloak]
        end
    end

    Users --> ALB
    ALB --> AdminUI
    ALB --> SignupUI
    ALB --> GQLGateway
    ALB --> Webhooks

    AdminUI --> GQLGateway
    SignupUI --> GQLGateway

    GQLGateway --> AuthAPI
    GQLGateway --> UserAPI
    GQLGateway --> ProductAPI
    GQLGateway --> PaymentAPI
    GQLGateway --> WarpCache

    CoreAPIs --> RDS
    CoreAPIs --> ElastiCache
    CoreAPIs --> MSK
    CoreAPIs --> Keycloak

    MSK --> KConsumers
    KConsumers --> StripeExt
    KConsumers --> TwilioExt
    KConsumers --> CoreAPIs

    StripeExt --> Webhooks
    TwilioExt --> Webhooks
    Webhooks --> MSK

    Workers --> LocalAPI
    Workers --> WarpCache
    Workers --> GraphMon
```

## Component Layers

### Client Layer

| Component | Purpose |
|-----------|---------|
| **Admin UI** | React app for clients to manage plans, view subscribers |
| **Signup UI** | React app for subscribers to sign up and manage subscriptions |

### API Layer

| Component | Purpose |
|-----------|---------|
| **GraphQL Gateway** | Federation gateway composing all GraphQL subgraphs |
| **AuthApi** | OTP and OAuth authentication, token management |
| **UserApi** | User, client, and subscriber management |
| **ProductApi** | Plans, pricing tiers, subscriptions, discounts |
| **PaymentApi** | Stripe checkout sessions, payment confirmations |

### Event Layer

| Component | Purpose |
|-----------|---------|
| **Kafka (MSK)** | Event streaming backbone |
| **KafkaConsumer** | Processes domain events, integrates with Stripe/Twilio |
| **Webhooks** | Receives external webhooks from Stripe and Twilio |

### Support Layer

| Component | Purpose |
|-----------|---------|
| **WarpCache** | High-performance gRPC caching (tokens, sessions) |
| **GraphMonitor** | GraphQL schema/endpoint registry |
| **LocalizationApi** | String translation for SMS messages |
| **Keycloak** | Identity provider (OIDC/OAuth2) |

### Data Layer

| Component | Purpose |
|-----------|---------|
| **PostgreSQL (RDS)** | Primary data store (userdb, productdb, paymentdb) |
| **Redis (ElastiCache)** | Caching, GraphQL subscriptions |

## Data Flow Patterns

### Authentication Flow

```mermaid
sequenceDiagram
    participant User
    participant UI
    participant AuthApi
    participant Keycloak
    participant WarpCache

    User->>UI: Enter phone number
    UI->>AuthApi: POST /otp
    AuthApi->>WarpCache: Store OTP secret
    AuthApi-->>User: SMS with code

    User->>UI: Enter OTP code
    UI->>AuthApi: POST /otp/verify
    AuthApi->>Keycloak: Create/authenticate user
    AuthApi->>WarpCache: Store JWT token
    AuthApi-->>UI: TokenId cookie
```

### Event-Driven Integration

```mermaid
sequenceDiagram
    participant API as ProductApi
    participant Kafka
    participant Consumer as KafkaConsumer
    participant Stripe
    participant Webhooks

    API->>Kafka: PlanCreatedEvent
    Kafka->>Consumer: Consume
    Consumer->>Stripe: Create Product

    Stripe->>Webhooks: product.created webhook
    Webhooks->>Kafka: ForeignProductSyncEvent
    Kafka->>Consumer: Consume
    Consumer->>API: Update plan with Stripe ID
```

### SMS Announcement Flow

```mermaid
sequenceDiagram
    participant Client
    participant Twilio
    participant Webhooks
    participant Consumer as KafkaConsumer
    participant LocalAPI as LocalizationApi

    Client->>Twilio: Send SMS "Hello subscribers"
    Twilio->>Webhooks: Incoming message webhook
    Webhooks-->>Client: "Reply CONFIRM to send"

    Client->>Twilio: Send SMS "CONFIRM"
    Twilio->>Webhooks: Incoming message webhook
    Webhooks->>Consumer: Get subscribers
    Consumer->>LocalAPI: Localize message
    Consumer->>Twilio: Send to all subscribers
    Webhooks-->>Client: "Sent to 5 subscribers"
```

## Deployment

All services run as containers in **AWS ECS** (Fargate), with:

- **Application Load Balancer** for HTTP/HTTPS traffic
- **Amazon MSK** for managed Kafka
- **Amazon RDS** for PostgreSQL
- **Amazon ElastiCache** for Redis
- **S3** for static site hosting and Terraform state
- **Route 53** for DNS
- **ACM** for SSL certificates
