# Screens

Documentation for each screen/route in the new-admin application.

## Table of Contents

- [Overview](#overview)
- [Login](#login)
- [Home](#home)
- [Plans](#plans)
- [Plan Editor](#plan-editor)
- [Discount Code Editor](#discount-code-editor)

## Overview

Each screen in the app follows a consistent pattern:

```
Screens/ScreenName/
├── index.tsx           # Main component
├── ScreenName.ts       # GraphQL query definition
├── route.ts            # Route configuration
├── hooks/              # Screen-specific hooks (if needed)
└── __generated__/      # Relay generated types
```

All screens except Login are protected by the `withAuthorization` HOC.

## Login

**Path:** `/login`
**File:** `src/Screens/Login/index.tsx`
**Protected:** No

### Purpose

Handles authentication redirect flow.

### Behavior

1. Queries `viewer` field from GraphQL
2. If `viewer.id` exists → user is logged in → redirect to `/`
3. If no `viewer.id` → redirect to `/auth/login?redirectUri={current_url}`

The actual authentication happens in the Auth API (external service). After successful auth, the user is redirected back to the app.

### GraphQL Query

```graphql
query LoginQuery {
  viewer {
    ... on Admin {
      id
    }
  }
}
```

### Implementation Notes

- Minimal UI (just shows `null` or redirect)
- Uses `window.location.href` for external redirect
- Auth API sets authentication cookie
- On successful auth, Auth API redirects back to `redirectUri`

## Home

**Path:** `/`
**File:** `src/Screens/Home/index.tsx`
**Protected:** Yes

### Purpose

Dashboard/landing page showing a list of plans.

### Features

- Lists all plans with links to edit them
- "View Plans" button → navigates to `/plans`

### GraphQL Query

```graphql
query HomeQuery {
  plans {
    edges {
      node {
        id
        name
      }
    }
  }
}
```

### UI Components

- Simple `<ul>` list with wouter `<Link>` components
- Each plan links to `/plan/{id}`
- Button to navigate to `/plans` screen

### Improvement Opportunities

- Add plan statistics (subscriber count, revenue, etc.)
- Add quick actions (create plan, view analytics)
- Display plan status indicators (active/inactive)

## Plans

**Path:** `/plans`
**File:** `src/Screens/Plans/index.tsx`
**Protected:** Yes

### Purpose

Lists all subscription plans with a "New Plan" button.

### Features

- Displays all plans as links
- "New Plan" button → navigates to `/plan` (no ID = create mode)

### GraphQL Query

```graphql
query PlansQuery {
  plans {
    edges {
      node {
        id
        name
      }
    }
  }
}
```

### UI Components

- Similar to Home screen but with "New Plan" action
- Each plan links to `/plan/{id}` for editing

### Difference from Home

- Home: Read-only view with general "View Plans" action
- Plans: Management view with "New Plan" creation

## Plan Editor

**Path:** `/plan/:id?`
**File:** `src/Screens/Plan/index.tsx`
**Protected:** Yes

### Purpose

Create or edit subscription plans with pricing tiers.

### Route Parameters

- `id` (optional): Plan ID to edit. If omitted, creates a new plan.

### Components

**Main component (`index.tsx`):**
- Renders `<PlanEditor>` component

**PlanEditor (`PlanEditor.tsx`):**
- Manages form state with React Hook Form + Zod
- Handles create/update/delete mutations
- Subscribes to plan updates for real-time sync

**PlanForm (`PlanForm.tsx`):**
- Renders form fields
- Manages price tier sub-forms
- Handles price tier mutations (create/delete/patch)

### GraphQL Operations

**Query:**
```graphql
query PlanQuery($id: ID) {
  plan(id: $id) {
    ...PlanEditor_plan
  }
}
```

**Fragment:**
```graphql
fragment PlanEditor_plan on Plan {
  id
  name
  description
  iconUrl
  isActive
  foreignServiceId
  features {
    planId
    maxMessages
    supportTier
    aiSupport
  }
  priceTiers {
    id
    planId
    name
    description
    price
    duration
    status
    iconUrl
    foreignServiceId
  }
}
```

**Mutations:**
- `createPlan`: Create new plan
- `updatePlan`: Update existing plan
- `deletePlan`: Delete plan
- `deletePriceTier`: Delete a price tier
- `patchPriceTier`: Update price tier (toggle active/inactive)

**Subscriptions:**
- `onPlanUpdated`: Real-time plan updates (e.g., from Stripe webhooks)
- `onPriceTierUpdated`: Real-time price tier updates

### Form Schema

The form uses Zod validation (`PlanEditor.tsx:38-94`):

**Plan fields:**
- `name`: Required, max 100 chars
- `description`: String
- `iconUrl`: Optional URL
- `isActive`: Boolean
- `foreignServiceId`: Read-only (Stripe product ID)

**Features:**
- `maxMessages`: Integer, min 1
- `supportTier`: "BASIC" | "STANDARD" | "PREMIUM"
- `aiSupport`: Boolean

**Price tiers (array):**
- `name`: Required, max 100 chars
- `price`: Number, min 1
- `duration`: "P7D" (weekly) | "P30D" (monthly) | "P365D" (yearly)
- `status`: "ACTIVE" | "INACTIVE" | "ARCHIVED" | "DELETED"
- `description`: Optional
- `iconUrl`: Optional URL
- `foreignServiceId`: Read-only (Stripe price ID)

### Features

**Create Mode** (no `id`):
- Empty form
- Submit creates plan via `createPlan` mutation
- On success: redirects to `/plan/{newId}` for editing

**Edit Mode** (with `id`):
- Form pre-filled with plan data
- Delete button in top-right corner
- Real-time sync via subscriptions
- Auto-saves when toggling `isActive` switch

**Price Tier Management:**
- Add new tiers with "Add Price Tier" button
- Delete unsaved tiers (trash icon)
- Deactivate saved tiers (power off icon)
- Reactivate inactive tiers (power on icon)
- Each tier has its own subscription for real-time updates

**Real-time Updates:**

When external events update the plan (e.g., Stripe webhook):
1. Subscription fires (`onPlanUpdated`)
2. Relay updates cache
3. `useEffect` in `PlanEditor` resets form with new data
4. Uses `keepDirtyValues: true` to preserve unsaved edits

**Error Handling:**
- Separate error states for create/update/delete operations
- `ErrorDialog` displays GraphQL errors
- Subscription errors shown in dedicated dialogs

### UI Layout

```
Card
├── Header
│   ├── Title: "Edit Plan {name}" or "Create Plan"
│   └── Delete Button (edit mode only)
└── Content
    └── Form
        ├── Basic Info (name, description, icon)
        ├── Active Toggle
        ├── Foreign Service ID (read-only)
        ├── Separator
        ├── Features
        │   ├── Max Messages
        │   ├── Support Tier
        │   └── AI Support
        ├── Separator
        ├── Price Tiers
        │   ├── Tier Cards (repeatable)
        │   │   ├── Name
        │   │   ├── Price
        │   │   ├── Duration
        │   │   ├── Status
        │   │   ├── Description
        │   │   ├── Icon URL
        │   │   └── Foreign Service ID
        │   └── Add Price Tier Button
        └── Save Changes Button
```

### Workflows

**Create a Plan:**
1. Navigate to `/plan`
2. Fill in plan details
3. Add price tiers
4. Click "Save Changes"
5. Redirects to `/plan/{id}` for further editing

**Edit a Plan:**
1. Navigate to `/plan/{id}`
2. Form loads with existing data
3. Make changes
4. Click "Save Changes" or toggle `isActive` (auto-saves)
5. Changes saved via `updatePlan` mutation

**Delete a Plan:**
1. In edit mode, click delete button (trash icon)
2. Mutation fires: `deletePlan`
3. On success: redirects to `/plans`

**Manage Price Tiers:**
- **Add**: Click "Add Price Tier" → new card appears
- **Edit**: Modify fields, submit form
- **Delete** (unsaved): Click trash icon → tier removed from form
- **Deactivate** (saved): Click power-off icon → tier status → INACTIVE
- **Reactivate** (inactive): Click power-on icon → tier status → ACTIVE

### Real-time Sync

The Plan Editor subscribes to updates via GraphQL subscriptions.

**Use case:** Stripe webhook updates plan

1. User creates plan in UI
2. Plan synced to Stripe (via KafkaConsumer)
3. Stripe webhook fires `product.updated`
4. Webhooks service → Kafka → ProductApi
5. ProductApi updates plan with `foreignServiceId`
6. GraphQL Gateway fires `onPlanUpdated` subscription
7. Plan Editor receives update
8. Form updates `foreignServiceId` field (read-only)

**Subscription hook** (`hooks/PlanUpdated.ts`):

```typescript
usePlanUpdatedSubscription(planId, (errors) => {
  // Handle subscription errors
});
```

Automatically updates Relay cache, triggering re-render.

### Known Issues

**TODO (`PlanEditor.tsx:65`):**
GraphQL errors from responses not currently passed to components. Only network errors are handled.

## Discount Code Editor

**Path:** `/discount-code/:id?`
**File:** `src/Screens/DiscountCode/index.tsx`
**Protected:** Yes (assumed)

### Purpose

Create or edit discount/coupon codes.

### Route Parameters

- `id` (optional): Discount code ID to edit. If omitted, creates new code.

### Components

**Main component (`index.tsx`):**
- Renders `<DiscountCodeEditor>` component

**DiscountCodeEditor (`DiscountCodeEditor.tsx`):**
- Manages discount code form
- Handles create/update mutations

**DiscountCodeForm (`DiscountCodeForm.tsx`):**
- Form fields for discount code properties

### GraphQL Operations

**Query:**
```graphql
query DiscountCodeQuery($id: ID) {
  discountCode(id: $id) {
    ...DiscountCodeEditor_discountCode
  }
}
```

**Fragment:**
```graphql
fragment DiscountCodeEditor_discountCode on DiscountCode {
  id
  code
  name
  # ... other fields
}
```

**Mutations:**
- `createDiscountCode`: Create new discount code
- `updateDiscountCode`: Update existing discount code

### Features

Similar pattern to Plan Editor:
- Create mode (no ID) vs Edit mode (with ID)
- Form validation with React Hook Form + Zod
- GraphQL mutations for CRUD operations

### Known Issues

**TODO (`DiscountCodeForm.tsx:46`):**
Missing dialog component for selecting price tiers to apply discount to.

**Implementation needed:**
- Price tier selection dialog
- Multi-select or checkbox list
- Link discount codes to specific price tiers

## Common Patterns

### Data Loading

All screens use the Relay integration pattern:

1. Define GraphQL query in `ScreenName.ts`
2. Query auto-loads based on route params
3. Data passed to component via `data` prop
4. Suspense shows loading state
5. Errors handled by error boundary

### Protected Routes

Most screens use `withAuthorization` HOC:

```typescript
// route.ts
export default {
  path: "/my-screen",
  component: withAuthorization(React.lazy(() => import("."))),
  // ...
};
```

This ensures users are authenticated before accessing the screen.

### Form Handling

Screens with forms follow this pattern:

1. Define Zod schema for validation
2. Setup React Hook Form with `zodResolver`
3. Use shadcn Form components for fields
4. Handle submit with Relay mutations
5. Show errors via `ErrorDialog` or toast

### Real-time Updates

Screens that need real-time sync:

1. Define GraphQL subscription
2. Create custom hook (in `hooks/` folder)
3. Call hook in edit mode
4. Relay automatically updates cache
5. `useEffect` resets form with new data

### Navigation

Navigation uses wouter hooks:

```typescript
import { useLocation } from "wouter";

const [location, navTo] = useLocation();

// Navigate programmatically
navTo("/plans");

// Link component
<Link href="/plan/123">Edit Plan</Link>
```

### Toast Notifications

Success/error notifications use Snackbar:

```typescript
import { useSnackbar } from "@/components/Snackbar";

const { showSnackbar } = useSnackbar();

showSnackbar("success", "Plan saved!", 3000);
showSnackbar("error", "Failed to save", 5000);
```

## Adding a New Screen

See [Development Guide - Creating a New Screen](DEVELOPMENT.md#creating-a-new-screen) for step-by-step instructions.

**Quick checklist:**

1. Create directory: `src/Screens/MyScreen/`
2. Create GraphQL query: `MyScreen.ts`
3. Create component: `index.tsx`
4. Create route definition: `route.ts`
5. Register route in `src/Screens/index.tsx`
6. Run `npm run relay` to generate types
7. Test navigation and data loading

## Navigation Flow

```
/login (unauthenticated)
  ↓
/ (Home) ← authenticated users land here
  ├── View plan → /plan/{id}
  └── View Plans → /plans
                    ├── Edit plan → /plan/{id}
                    └── New Plan → /plan
```

**External routes:**
- `/auth/login` - Auth API login page
- `/auth/logout` - Auth API logout endpoint

## Future Enhancements

**Home Screen:**
- Plan statistics dashboard
- Quick actions (create, search)
- Recent activity feed

**Plans Screen:**
- Search/filter plans
- Sort by name, date, status
- Bulk actions (activate/deactivate)

**Plan Editor:**
- Duplicate plan
- Plan templates
- Preview mode
- Validation warnings (e.g., no active price tiers)

**Discount Code Editor:**
- Price tier selector dialog
- Discount analytics
- Expiration date picker
- Usage limits

**New Screens:**
- Clients management (`/clients`)
- Subscribers management (`/subscribers`)
- Analytics dashboard (`/analytics`)
- Settings (`/settings`)

## Related Documentation

- [Development Guide](DEVELOPMENT.md) - How to build new screens
- [Architecture](ARCHITECTURE.md) - Relay integration and routing
- [Components](COMPONENTS.md) - UI components for building screens
