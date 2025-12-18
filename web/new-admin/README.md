# New Admin Portal

Modern admin interface for the Nudges platform, built with React, TypeScript, Relay, and shadcn/ui.

## What It Does

The new-admin portal provides administrative capabilities for managing:

- **Plans**: Create and manage subscription plans with pricing tiers
- **Discount Codes**: Configure promotional discount codes
- **Authentication**: OAuth-based admin authentication via Auth API

This replaces the legacy admin interface with a modern, type-safe implementation using GraphQL subscriptions for real-time updates.

## Tech Stack

- **Framework**: React 18 + TypeScript
- **Build Tool**: Vite 5
- **GraphQL Client**: Relay (with subscriptions via WebSocket)
- **Routing**: Wouter (lightweight, hook-based)
- **UI Library**: shadcn/ui (Radix UI + Tailwind CSS)
- **Forms**: React Hook Form + Zod validation
- **Styling**: Tailwind CSS with dark mode support

## Quick Start

### Prerequisites

- Node.js 18+ and npm
- Running GraphQL Gateway (port 5443)
- Running Auth API (port 5555)
- SSL certificates in parent directory (`aspnetapp.key` and `aspnetapp.crt`)

### Installation

```bash
npm install
```

### Development

```bash
# Start dev server (with HTTPS)
npm run dev

# Regenerate Relay artifacts after schema changes
npm run relay

# Lint
npm run lint

# Build for production
npm run build
```

The app runs at `https://localhost:5050` and proxies:
- `/graphql` → GraphQL Gateway (port 5443) with WebSocket support
- `/auth/*` → Auth API (port 5555)

### Authentication

The app integrates with the Auth API OAuth flow:
1. Unauthenticated users are redirected to `/auth/login`
2. After successful auth, users are redirected back to the app
3. GraphQL requests use cookie-based auth from Auth API

**Default credentials**: See [main project documentation](https://docs.nudges.dev)

## Project Structure

```
web/new-admin/
├── src/
│   ├── components/        # Reusable UI components
│   │   ├── ui/           # shadcn/ui components
│   │   └── *.tsx         # Custom components
│   ├── Router/           # Routing infrastructure
│   │   ├── createRouterFactory.tsx  # Route factory with param extraction
│   │   └── withRelay.tsx            # HOC for Relay integration
│   ├── Screens/          # Route screens
│   │   ├── Home/
│   │   ├── Login/
│   │   ├── Plans/
│   │   ├── Plan/
│   │   └── DiscountCode/
│   ├── AuthProvider.tsx           # Auth state and withAuthorization HOC
│   ├── RelayEnvironmentProviderWrapper.tsx
│   ├── crateRelayEnvironment.ts   # Relay setup (fetch + subscriptions)
│   └── main.tsx
├── schema.graphql        # GraphQL schema (for Relay compiler)
├── relay.config.json     # Relay compiler config
└── vite.config.ts        # Vite config with proxy setup
```

## Key Patterns

### Screen/Route Pattern

Each screen follows a consistent structure:

```
Screens/ScreenName/
├── index.tsx           # React component
├── ScreenName.ts       # GraphQL query (Relay fragment)
├── route.ts            # Route definition
└── __generated__/      # Relay generated types
```

**Example route definition** (`route.ts`):

```typescript
import query, { gql } from './ScreenName.ts';
import Component from './index.tsx';

export default {
  path: '/screen/:id',
  query,
  gqlQuery: gql,
  component: Component,
  fetchPolicy: 'store-or-network'
};
```

### Relay Integration

The app uses a custom Relay integration that:
- Automatically loads GraphQL queries based on route params
- Provides query refresh capabilities via `useRelayScreenContext()`
- Supports real-time updates via GraphQL subscriptions
- Shows loading skeletons during data fetching

**Access Relay data in components**:

```typescript
import { RelayRoute } from '@/Router/withRelay';
import type { MyScreenQuery } from './__generated__/MyScreenQuery.graphql';

export default function MyScreen({ data }: Readonly<RelayRoute<MyScreenQuery>>) {
  // data contains your GraphQL query result
  return <div>{data.myField}</div>;
}
```

**Refresh queries**:

```typescript
import { useRelayScreenContext } from '@/Router/withRelay';

function MyComponent() {
  const { refresh, variables } = useRelayScreenContext<MyScreenQuery>();

  const handleRefresh = () => {
    refresh({ ...variables, newParam: 'value' });
  };
}
```

### Form Handling

Forms use React Hook Form + Zod for validation:

```typescript
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';

const schema = z.object({
  name: z.string().min(1, 'Name required'),
});

function MyForm() {
  const form = useForm({
    resolver: zodResolver(schema),
  });

  return <Form {...form}>...</Form>;
}
```

### Authentication

Use the `withAuthorization` HOC to protect routes:

```typescript
import { withAuthorization } from '@/AuthProvider';

export default withAuthorization(MyProtectedScreen);
```

This automatically:
- Checks for valid admin session via GraphQL
- Redirects to `/login` if unauthorized
- Shows loading state during auth check

## Configuration

### Vite Proxy

The dev server proxies backend services (see `vite.config.ts:28`):

- GraphQL Gateway: `https://localhost:5443`
- Auth API: `https://localhost:5555`

Update these if your backend runs on different ports.

### Relay Compiler

Schema location: `schema.graphql`

The schema is generated from the GraphQL Gateway. To update:
1. Export schema from gateway
2. Replace `schema.graphql`
3. Run `npm run relay`

## Development Workflow

1. **Start backend services** (GraphQL Gateway + Auth API)
2. **Start dev server**: `npm run dev`
3. **Make changes** to components or add new screens
4. **Update GraphQL**: If changing queries, run `npm run relay`
5. **Build**: `npm run build` (outputs to `dist/`)

## Docker Deployment

The app includes a Dockerfile with nginx config for production:

```dockerfile
# Build stage
FROM node:18 AS build
COPY . .
RUN npm ci && npm run build

# Runtime stage
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY admin-ui.nginx.conf /etc/nginx/conf.d/default.conf
```

## Known Limitations

- GraphQL errors from responses not currently passed to components (see `withRelay.tsx:65`)
- Discount code editor missing price tier dialog (see `DiscountCodeForm.tsx:46`)

## Further Documentation

- [Architecture](docs/ARCHITECTURE.md) - Detailed architectural patterns
- [Development](docs/DEVELOPMENT.md) - Development guidelines and workflows
- [Components](docs/COMPONENTS.md) - Component library and patterns
- [Screens](docs/SCREENS.md) - Screen-by-screen documentation
- [Storybook](STORYBOOK.md) - Interactive component and pattern showcase

## Storybook

This project includes a Storybook showcasing the custom Relay integration patterns and component library.

**Run locally:**
```bash
npm run storybook
```

**Build for deployment:**
```bash
npm run build-storybook
```

The Storybook demonstrates:
- Custom Relay + Wouter routing integration
- Real-time subscription patterns
- shadcn/ui component library
- Form patterns with React Hook Form + Zod

See [STORYBOOK.md](STORYBOOK.md) for details.

## Related

- Main project: [Nudges System Documentation](https://docs.nudges.dev)
- GraphQL Gateway: [services/graphql-gateway.md](https://docs.nudges.dev/services/graphql-gateway)
- Auth API: [services/auth-api.md](https://docs.nudges.dev/services/auth-api)
