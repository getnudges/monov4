# Storybook Setup

This project includes a Storybook setup showcasing the custom Relay integration patterns and component library.

## Running Storybook

### Development Mode

```bash
npm run storybook
```

Opens Storybook at `http://localhost:6006`

### Build Static Version

```bash
npm run build-storybook
```

Builds static Storybook to `../../mkdocs/site/admin-storybook/`

This can be hosted alongside the mkdocs documentation at `docs.nudges.dev/admin-storybook/`

## What's Included

### Introduction
- Overview of the custom Relay patterns
- Explanation of the routing architecture
- Tech stack summary

### Components
Stories for shadcn/ui components:
- Button - All variants and sizes
- Card - Various layouts and use cases
- (More to be added)

### Patterns
The unique Relay integration patterns:

1. **Relay Routing** - How URL params automatically become query variables
2. **Relay Screen Example** - Interactive demo of a screen with data loading
3. **Real-time Subscriptions** - WebSocket subscription patterns

Each pattern includes:
- Explanation of how it works
- Code examples
- Interactive demos
- Benefits and use cases

## Adding New Stories

### Component Story

Create `src/stories/Components/MyComponent.stories.tsx`:

```typescript
import type { Meta, StoryObj } from "@storybook/react";
import { MyComponent } from "@/components/MyComponent";

const meta = {
  title: "Components/MyComponent",
  component: MyComponent,
  tags: ["autodocs"],
} satisfies Meta<typeof MyComponent>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    // Component props
  },
};
```

### Pattern Documentation

Create `src/stories/Patterns/MyPattern.mdx`:

```mdx
import { Meta } from "@storybook/blocks";

<Meta title="Patterns/My Pattern" />

# My Pattern

Explanation of the pattern...

## Code Example

\`\`\`typescript
// Code here
\`\`\`
```

## Relay Mock Utilities

For stories that need Relay environment:

```typescript
import { withMockRelay } from "../../../.storybook/relay-mock";

export const MyStory: Story = {
  decorators: [
    withMockRelay({
      // Mock GraphQL response
      plan: { id: "1", name: "Basic" }
    })
  ],
  args: {
    data: { plan: { id: "1", name: "Basic" } }
  },
};
```

## Deployment

The built Storybook is output to `mkdocs/site/admin-storybook/` for deployment alongside the main documentation.

**Hosting options:**
1. Subdirectory: `docs.nudges.dev/admin-storybook/`
2. Subdomain: `storybook.nudges.dev`

To deploy:
1. Run `npm run build-storybook`
2. Deploy the mkdocs site (includes Storybook in subdirectory)

## Configuration

- **Main config**: `.storybook/main.ts`
- **Preview config**: `.storybook/preview.tsx`
- **Relay mocks**: `.storybook/relay-mock.tsx`

The preview includes decorators for:
- ThemeProvider (dark/light mode)
- Router (wouter)
- Basic padding/layout

## Why Storybook?

This Storybook serves multiple purposes:

1. **Pattern Showcase** - Demonstrates the custom Relay integration patterns
2. **Component Library** - Documents the shadcn/ui component usage
3. **Developer Reference** - Shows how to build screens following the patterns
4. **Portfolio Piece** - Showcases architectural decisions (for OSS/hiring)

The Relay patterns are particularly unique and worth highlighting!

## Future Enhancements

- Add more component stories (Form, Input, Select, etc.)
- Add Form + Mutation pattern story
- Add Auth HOC pattern story
- Add real examples from actual screens (Plan Editor, etc.)
- Add visual regression testing
- Add interaction tests with @storybook/test
