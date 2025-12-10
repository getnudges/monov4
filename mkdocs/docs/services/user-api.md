# UserApi

UserApi is a GraphQL subgraph that manages users, clients, and subscribers.

## Entities

| Entity | Description |
|--------|-------------|
| **Admin** | System administrators |
| **Client** | Service providers/businesses with subscribers |
| **Subscriber** | End-users who subscribe to clients |
| **User** | Underlying user record (phone, locale, subject) |

## GraphQL Operations

### Queries

| Query | Auth | Description |
|-------|------|-------------|
| `getClient(id)` | Admin | Fetch client by ID |
| `getClientByPhoneNumber(phone)` | Admin | Lookup by phone hash |
| `getClientBySlug(slug)` | Any | Public lookup by slug |
| `getClientByCustomerId(id)` | Admin | Find by Stripe customer ID |
| `getClients()` | Admin | List all clients |
| `getSubscribers()` | Admin/Client | List subscribers |
| `totalClients()` | Admin | Count clients |
| `totalSubscribers()` | Admin | Count subscribers |
| `viewer` | Any | Current authenticated user |

### Mutations

| Mutation | Auth | Description |
|----------|------|-------------|
| `createClient(input)` | Client | Create client from authenticated user |
| `updateClient(input)` | Admin/Client | Update client details |
| `deleteClient(id)` | Admin | Remove client |
| `subscribeToClient(clientId)` | Subscriber | Join client's subscriber list |
| `unsubscribeFromClient(clientId)` | Subscriber | Leave client |
| `deleteSubscriber(id)` | Admin | Remove subscriber |

### Subscriptions (WebSocket)

- `onClientUpdated(id)` - Real-time client updates
- `clientCreated` - New client created
- `subscriberUnsubscribed` - Subscriber left

## Configuration

```ini
ConnectionStrings__UserDb=Host=localhost;Database=userdb;...
HashSettings__HashKeyBase64=<base64-key>
EncryptionSettings__Key=<base64-key>
Kafka__BrokerList=kafka:9092
REDIS_URL=redis:6379
Otlp__Endpoint=http://otel-collector:4317
```

## Security

- Phone numbers are hashed (HMAC) for lookups
- Sensitive fields use AES-GCM encryption
- JWT authentication via Keycloak

## Kafka Events

Publishes to topics: `clients`, `notifications`

## Running

```powershell
cd dotnet/UserApi
dotnet run
```

Default port: `5300`
