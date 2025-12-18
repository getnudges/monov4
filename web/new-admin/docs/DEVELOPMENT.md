# Development Guide

This guide covers development workflows, conventions, and best practices for working on the new-admin app.

## Table of Contents

- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Creating a New Screen](#creating-a-new-screen)
- [Working with GraphQL](#working-with-graphql)
- [Adding UI Components](#adding-ui-components)
- [Form Development](#form-development)
- [Styling Guidelines](#styling-guidelines)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)

## Getting Started

### Environment Setup

1. **Install dependencies:**
   ```bash
   npm install
   ```

2. **Verify backend services are running:**
   - GraphQL Gateway: `https://localhost:5443/graphql`
   - Auth API: `https://localhost:5555`

3. **Ensure SSL certificates exist:**
   ```bash
   ls ../aspnetapp.key ../aspnetapp.crt
   ```

   These should be in the parent directory. If missing, see main project docs for certificate generation.

4. **Start dev server:**
   ```bash
   npm run dev
   ```

5. **Open app:**
   Navigate to `https://localhost:5050`

### IDE Setup

**Recommended VSCode extensions:**

- ESLint
- Tailwind CSS IntelliSense
- Relay GraphQL
- TypeScript and JavaScript Language Features (built-in)

**VSCode settings (`.vscode/settings.json`):**

```json
{
  "editor.formatOnSave": true,
  "editor.defaultFormatter": "esbenp.prettier-vscode"
}
```

## Development Workflow

### Daily Workflow

1. **Pull latest changes:**
   ```bash
   git pull origin main
   npm install  # If package.json changed
   ```

2. **Start dev server:**
   ```bash
   npm run dev
   ```

3. **Make changes** (hot reload enabled)

4. **If GraphQL changes:**
   ```bash
   npm run relay  # Regenerate types
   ```

5. **Lint before committing:**
   ```bash
   npm run lint
   ```

### Build and Preview

**Production build:**
```bash
npm run build
```

Output goes to `dist/` directory.

**Preview production build:**
```bash
npm run preview
```

Serves the `dist/` folder locally.

## Creating a New Screen

Follow these steps to add a new screen/route:

### 1. Create Screen Directory

```bash
mkdir -p src/Screens/MyScreen
cd src/Screens/MyScreen
```

### 2. Create GraphQL Query (`MyScreen.ts`)

```typescript
import { graphql } from "react-relay";

export const MyScreenQueryDef = graphql`
  query MyScreenQuery($id: ID!) {
    myData(id: $id) {
      id
      name
      # Add fields you need
    }
  }
`;
```

**Important:** The query name MUST match the filename pattern: `{ScreenName}Query`

### 3. Create Component (`index.tsx`)

```typescript
import { RelayRoute } from "@/Router/withRelay";
import type { MyScreenQuery } from "./__generated__/MyScreenQuery.graphql";

export default function MyScreen({
  data
}: Readonly<RelayRoute<MyScreenQuery>>) {
  return (
    <div>
      <h1>{data.myData?.name}</h1>
      {/* Your UI here */}
    </div>
  );
}
```

### 4. Create Route Definition (`route.ts`)

```typescript
import { RouteDefinition } from "@/Router/withRelay";
import Query, { type MyScreenQuery } from "./__generated__/MyScreenQuery.graphql";
import { MyScreenQueryDef } from "./MyScreen";
import { withAuthorization } from "@/AuthProvider";
import React from "react";

export default {
  path: "/my-screen/:id",
  component: withAuthorization(React.lazy(() => import("."))),
  gqlQuery: MyScreenQueryDef,
  query: Query,
  fetchPolicy: 'store-or-network'  // Optional
} satisfies RouteDefinition<MyScreenQuery>;
```

**Route options:**

- `path`: Wouter path pattern (supports `:param`, `/:optional?`)
- `component`: React component (wrap with `withAuthorization` if protected)
- `gqlQuery`: GraphQL query definition
- `query`: Relay-compiled query (from `__generated__`)
- `fetchPolicy`: `'store-or-network'` | `'store-and-network'` | `'network-only'`

### 5. Register Route

Add to `src/Screens/index.tsx`:

```typescript
import MyScreenRoute from "./MyScreen/route";

export const routes = [
  LoginRoute,
  HomeRoute,
  MyScreenRoute,  // Add here
  // ... other routes
];
```

### 6. Generate Relay Types

```bash
npm run relay
```

This generates `src/Screens/MyScreen/__generated__/MyScreenQuery.graphql.ts`

### 7. Test

Navigate to `/my-screen/123` and verify:
- Query loads with correct variables
- Data renders correctly
- Loading state shows during fetch
- Errors display properly

## Working with GraphQL

### Schema Updates

When the GraphQL Gateway schema changes:

1. **Export new schema** from Gateway to `schema.graphql`
2. **Regenerate types:**
   ```bash
   npm run relay
   ```
3. **Check TypeScript errors** in your queries/components
4. **Update code** to match new schema

### Query Best Practices

**Use fragments for reusable data:**

```typescript
// PlanEditor.tsx
const PlanEditorFragment = graphql`
  fragment PlanEditor_plan on Plan {
    id
    name
    description
    priceTiers {
      id
      price
    }
  }
`;

// PlanQuery.ts
const PlanQueryDef = graphql`
  query PlanQuery($id: ID!) {
    plan(id: $id) {
      ...PlanEditor_plan
    }
  }
`;
```

**Benefits:**
- Colocation: Data needs next to component
- Reusability: Share fragments across queries
- Type safety: Relay validates fragment usage

### Mutations

**Define mutation:**

```typescript
const CreatePlanMutation = graphql`
  mutation MyScreenCreatePlanMutation($input: CreatePlanInput!) {
    createPlan(input: $input) {
      plan {
        id
        name
      }
    }
  }
`;
```

**Use in component:**

```typescript
import { useMutation } from 'react-relay';

function MyComponent() {
  const [commit, isInFlight] = useMutation(CreatePlanMutation);

  const handleCreate = (data) => {
    commit({
      variables: { input: data },
      onCompleted: (response) => {
        // Success
      },
      onError: (error) => {
        // Error
      }
    });
  };

  return <button disabled={isInFlight}>Create</button>;
}
```

### Subscriptions

**Define subscription:**

```typescript
const PlanUpdatedSubscription = graphql`
  subscription MyScreenPlanUpdatedSubscription($id: ID!) {
    onPlanUpdated(id: $id) {
      id
      name
    }
  }
`;
```

**Use in component:**

```typescript
import { useSubscription } from 'react-relay';

function MyComponent({ planId }) {
  useSubscription({
    subscription: PlanUpdatedSubscription,
    variables: { id: planId },
    onNext: (response) => {
      // Relay automatically updates cache
      console.log('Plan updated:', response.onPlanUpdated);
    }
  });

  return <div>Subscribed to updates</div>;
}
```

### Refreshing Queries

**Refresh current screen's query:**

```typescript
import { useRelayScreenContext } from '@/Router/withRelay';

function MyComponent() {
  const { refresh, variables } = useRelayScreenContext<MyScreenQuery>();

  const handleRefresh = () => {
    refresh(variables);  // Re-fetch with same variables
    // Or with new variables:
    // refresh({ ...variables, newParam: 'value' });
  };

  return <button onClick={handleRefresh}>Refresh</button>;
}
```

## Adding UI Components

### Using shadcn/ui Components

The app uses shadcn/ui components located in `src/components/ui/`.

**Add a new shadcn component:**

```bash
npx shadcn@latest add button
```

This copies the component to `src/components/ui/button.tsx`.

**Available components:**

Already installed:
- Button, Card, Input, Label, Select, Switch
- Alert, AlertDialog, Toast/Toaster
- Form (React Hook Form integration)
- Dropdown, Sheet, Separator, Textarea

**Usage:**

```typescript
import { Button } from "@/components/ui/button";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card";

function MyComponent() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>Title</CardTitle>
      </CardHeader>
      <CardContent>
        <Button>Click me</Button>
      </CardContent>
    </Card>
  );
}
```

### Creating Custom Components

**For reusable components:**

```typescript
// src/components/MyComponent.tsx
import { cn } from "@/lib/utils";

interface MyComponentProps {
  title: string;
  className?: string;
}

export function MyComponent({ title, className }: MyComponentProps) {
  return (
    <div className={cn("p-4 bg-card", className)}>
      <h2>{title}</h2>
    </div>
  );
}
```

**For screen-specific components:**

Keep them in the screen folder:

```
Screens/MyScreen/
├── index.tsx
├── MyScreenForm.tsx    # Screen-specific
└── MyScreenEditor.tsx  # Screen-specific
```

## Form Development

### Creating a Form

**1. Define Zod schema:**

```typescript
import * as z from "zod";

const formSchema = z.object({
  name: z.string().min(1, "Name is required"),
  email: z.string().email("Invalid email"),
  age: z.number().min(18, "Must be 18+"),
});

type FormData = z.infer<typeof formSchema>;
```

**2. Setup React Hook Form:**

```typescript
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";

function MyForm() {
  const form = useForm<FormData>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      name: "",
      email: "",
      age: 18,
    },
  });

  const onSubmit = (data: FormData) => {
    // Data is validated and typed
    console.log(data);
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)}>
        {/* Fields */}
      </form>
    </Form>
  );
}
```

**3. Add form fields:**

```typescript
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";

<FormField
  control={form.control}
  name="name"
  render={({ field }) => (
    <FormItem>
      <FormLabel>Name</FormLabel>
      <FormControl>
        <Input placeholder="Enter name" {...field} />
      </FormControl>
      <FormDescription>Your full name</FormDescription>
      <FormMessage />  {/* Validation errors */}
    </FormItem>
  )}
/>
```

### Form with Mutation

**Complete example:**

```typescript
import { useMutation } from "react-relay";
import { useForm } from "react-hook-form";

function CreatePlanForm() {
  const [commit, isInFlight] = useMutation(CreatePlanMutation);

  const form = useForm<FormData>({
    resolver: zodResolver(formSchema),
  });

  const onSubmit = (data: FormData) => {
    commit({
      variables: { input: data },
      onCompleted: () => {
        form.reset();
        // Show success toast
      },
      onError: (error) => {
        // Show error
      }
    });
  };

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)}>
        {/* Fields */}
        <Button type="submit" disabled={isInFlight}>
          {isInFlight ? "Creating..." : "Create"}
        </Button>
      </form>
    </Form>
  );
}
```

## Styling Guidelines

### Tailwind CSS

**Utility-first approach:**

```typescript
<div className="flex items-center gap-4 p-6 bg-card rounded-lg border">
  <h2 className="text-2xl font-bold">Title</h2>
</div>
```

**Use design tokens:**

- Colors: `bg-background`, `text-foreground`, `bg-card`, `border`
- Spacing: `p-4`, `m-2`, `gap-4`
- Typography: `text-sm`, `font-medium`, `leading-tight`

**Dark mode support:**

```typescript
<div className="bg-white dark:bg-gray-900 text-black dark:text-white">
  Content
</div>
```

Dark mode is handled by `ThemeProvider` using CSS variables.

### Using `cn()` Utility

For conditional/merged classes:

```typescript
import { cn } from "@/lib/utils";

<div className={cn(
  "base classes",
  isActive && "active classes",
  className  // Allow override via props
)}>
```

### Component Variants with CVA

For components with multiple variants:

```typescript
import { cva, type VariantProps } from "class-variance-authority";

const buttonVariants = cva(
  "inline-flex items-center justify-center rounded-md",
  {
    variants: {
      variant: {
        default: "bg-primary text-primary-foreground",
        outline: "border border-input bg-transparent",
      },
      size: {
        default: "h-10 px-4 py-2",
        sm: "h-9 px-3",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
);

interface ButtonProps extends VariantProps<typeof buttonVariants> {
  // ...
}
```

## Testing

Currently, the app doesn't have a test suite configured.

**Recommended setup:**

1. **Install Vitest:**
   ```bash
   npm install -D vitest @testing-library/react @testing-library/jest-dom
   ```

2. **Add test script:**
   ```json
   "scripts": {
     "test": "vitest"
   }
   ```

3. **Write tests:**
   ```typescript
   import { describe, it, expect } from 'vitest';
   import { render, screen } from '@testing-library/react';

   describe('MyComponent', () => {
     it('renders correctly', () => {
       render(<MyComponent />);
       expect(screen.getByText('Hello')).toBeInTheDocument();
     });
   });
   ```

## Troubleshooting

### Common Issues

#### "Module not found" errors

**Solution:**
```bash
rm -rf node_modules package-lock.json
npm install
```

#### Relay types out of sync

**Symptoms:** TypeScript errors about missing/incorrect generated types

**Solution:**
```bash
npm run relay
```

Always run after changing GraphQL queries or updating schema.

#### Port 5050 already in use

**Solution:**
```bash
# Kill process on port 5050
npx kill-port 5050

# Or change port in vite.config.ts
server: {
  port: 3000,  // Different port
}
```

#### GraphQL queries return null

**Check:**
1. Backend services running?
2. Proxy config correct in `vite.config.ts`?
3. Auth cookies valid? (try logging out/in)

**Debug:**
```typescript
// Add to crateRelayEnvironment.ts
console.log('GraphQL request:', operation, variables);
console.log('GraphQL response:', json);
```

#### WebSocket connection fails

**Check:**
1. GraphQL Gateway supports subscriptions
2. WebSocket proxy config: `ws: true` in `vite.config.ts`
3. Browser console for WebSocket errors

**Debug:**
```typescript
// In crateRelayEnvironment.ts
wsClient.on('connected', () => console.log('WS connected'));
wsClient.on('closed', () => console.log('WS closed'));
```

#### SSL certificate errors

**Symptoms:** `ERR_CERT_AUTHORITY_INVALID` or similar

**Solution:**
1. Regenerate certificates (see main project docs)
2. Trust certificates in OS keychain
3. Or disable cert validation (dev only):
   ```typescript
   // vite.config.ts
   proxy: {
     '/graphql': {
       secure: false,  // Disable cert validation
     }
   }
   ```

### Hot Reload Not Working

**Check:**
1. Is file saved?
2. Is file in `src/` directory?
3. Vite dev server running?

**Restart dev server:**
```bash
# Ctrl+C to stop
npm run dev
```

### Build Failures

**Common causes:**
- TypeScript errors
- Missing Relay types (run `npm run relay`)
- Unused imports (remove or disable ESLint rule)

**Debug:**
```bash
npm run build -- --mode development
```

Shows more detailed errors.

## Code Style

### TypeScript

- Use explicit types for function parameters
- Rely on inference for simple variables
- Prefer `type` over `interface` for component props

```typescript
// Good
type MyProps = {
  name: string;
  onClick: (id: string) => void;
};

// Avoid
interface MyProps {
  name: string;
}
```

### React

- Use functional components only
- Prefer hooks over class components
- Destructure props in function signature
- Use `Readonly<>` wrapper for prop types

```typescript
// Good
export default function MyComponent({
  name,
  onClick
}: Readonly<MyProps>) {
  // ...
}

// Avoid
export default function MyComponent(props: MyProps) {
  const name = props.name;
}
```

### Naming Conventions

- Components: `PascalCase` (e.g., `MyComponent.tsx`)
- Hooks: `camelCase` with `use` prefix (e.g., `useMyHook.ts`)
- Utilities: `camelCase` (e.g., `formatDate.ts`)
- Constants: `UPPER_SNAKE_CASE` (e.g., `API_URL`)

### File Organization

- One component per file
- Colocate related files (queries, types, styles)
- Use index.tsx for default exports
- Keep files under 300 lines (split if larger)

## Next Steps

- [Architecture](ARCHITECTURE.md) - Understand architectural patterns
- [Components](COMPONENTS.md) - Component library reference
- [Screens](SCREENS.md) - Screen-by-screen documentation
