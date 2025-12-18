import type { Meta, StoryObj } from "@storybook/react";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { withMockRelay } from "../../../.storybook/relay-mock";

// Mock implementation showing what a Relay screen looks like
interface MockPlanData {
  plan: {
    id: string;
    name: string;
    description: string;
    isActive: boolean;
    features: {
      maxMessages: number;
      supportTier: string;
      aiSupport: boolean;
    };
    priceTiers: Array<{
      id: string;
      name: string;
      price: number;
      duration: string;
    }>;
  };
}

// Simulates what PlanScreen component receives
function MockPlanScreen({ data }: { data: MockPlanData }) {
  return (
    <div className="container mx-auto py-10">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center justify-between">
            <span>{data.plan.name}</span>
            <span className="text-sm font-normal">
              {data.plan.isActive ? (
                <span className="text-green-500">● Active</span>
              ) : (
                <span className="text-gray-500">○ Inactive</span>
              )}
            </span>
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-6">
          <div>
            <h3 className="font-medium mb-2">Description</h3>
            <p className="text-muted-foreground">{data.plan.description}</p>
          </div>

          <div>
            <h3 className="font-medium mb-2">Features</h3>
            <ul className="space-y-1 text-sm">
              <li>Max Messages: {data.plan.features.maxMessages}</li>
              <li>Support Tier: {data.plan.features.supportTier}</li>
              <li>AI Support: {data.plan.features.aiSupport ? "Yes" : "No"}</li>
            </ul>
          </div>

          <div>
            <h3 className="font-medium mb-2">Price Tiers</h3>
            <div className="grid gap-2">
              {data.plan.priceTiers.map((tier) => (
                <Card key={tier.id}>
                  <CardContent className="pt-4">
                    <div className="flex justify-between items-center">
                      <div>
                        <div className="font-medium">{tier.name}</div>
                        <div className="text-sm text-muted-foreground">
                          {tier.duration === "P7D" && "Weekly"}
                          {tier.duration === "P30D" && "Monthly"}
                          {tier.duration === "P365D" && "Yearly"}
                        </div>
                      </div>
                      <div className="text-xl font-bold">${tier.price}</div>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

const meta = {
  title: "Patterns/Relay Screen Example",
  component: MockPlanScreen,
  parameters: {
    layout: "fullscreen",
  },
  tags: ["autodocs"],
} satisfies Meta<typeof MockPlanScreen>;

export default meta;
type Story = StoryObj<typeof meta>;

// Mock data that would come from GraphQL
const mockPlanData: MockPlanData = {
  plan: {
    id: "plan-123",
    name: "Premium Plan",
    description: "Our most popular plan with all the features you need",
    isActive: true,
    features: {
      maxMessages: 10000,
      supportTier: "PREMIUM",
      aiSupport: true,
    },
    priceTiers: [
      {
        id: "tier-1",
        name: "Weekly",
        price: 9.99,
        duration: "P7D",
      },
      {
        id: "tier-2",
        name: "Monthly",
        price: 29.99,
        duration: "P30D",
      },
      {
        id: "tier-3",
        name: "Yearly",
        price: 299.99,
        duration: "P365D",
      },
    ],
  },
};

export const LoadedWithData: Story = {
  decorators: [withMockRelay(mockPlanData)],
  args: {
    data: mockPlanData,
  },
};

export const BasicPlan: Story = {
  decorators: [
    withMockRelay({
      plan: {
        id: "plan-basic",
        name: "Basic Plan",
        description: "Perfect for getting started",
        isActive: true,
        features: {
          maxMessages: 100,
          supportTier: "BASIC",
          aiSupport: false,
        },
        priceTiers: [
          {
            id: "tier-1",
            name: "Monthly",
            price: 5.99,
            duration: "P30D",
          },
        ],
      },
    }),
  ],
  args: {
    data: {
      plan: {
        id: "plan-basic",
        name: "Basic Plan",
        description: "Perfect for getting started",
        isActive: true,
        features: {
          maxMessages: 100,
          supportTier: "BASIC",
          aiSupport: false,
        },
        priceTiers: [
          {
            id: "tier-1",
            name: "Monthly",
            price: 5.99,
            duration: "P30D",
          },
        ],
      },
    },
  },
};

export const InactivePlan: Story = {
  args: {
    data: {
      plan: {
        ...mockPlanData.plan,
        isActive: false,
        name: "Deprecated Plan",
        description: "This plan is no longer available for new subscriptions",
      },
    },
  },
};

// Code example showing the real implementation
export const HowItWorks = {
  render: () => (
    <div className="max-w-4xl mx-auto p-8 space-y-6">
      <h1 className="text-3xl font-bold">How This Pattern Works</h1>

      <Card>
        <CardHeader>
          <CardTitle>1. Route Definition</CardTitle>
        </CardHeader>
        <CardContent>
          <pre className="bg-muted p-4 rounded text-sm overflow-x-auto">
            {`// Screens/Plan/route.ts
export default {
  path: "/plan/:id",
  query: PlanQuery,
  gqlQuery: PlanQueryDef,
  component: withAuthorization(PlanScreen),
  fetchPolicy: 'store-or-network'
};`}
          </pre>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>2. GraphQL Query</CardTitle>
        </CardHeader>
        <CardContent>
          <pre className="bg-muted p-4 rounded text-sm overflow-x-auto">
            {`// Screens/Plan/Plan.ts
export const PlanQueryDef = graphql\`
  query PlanQuery($id: ID!) {
    plan(id: $id) {
      id
      name
      description
      isActive
      features { ... }
      priceTiers { ... }
    }
  }
\`;`}
          </pre>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>3. Component</CardTitle>
        </CardHeader>
        <CardContent>
          <pre className="bg-muted p-4 rounded text-sm overflow-x-auto">
            {`// Screens/Plan/index.tsx
export default function PlanScreen({
  data
}: Readonly<RelayRoute<PlanQuery>>) {
  // data is fully typed and loaded!
  return <div>{data.plan?.name}</div>;
}`}
          </pre>
        </CardContent>
      </Card>

      <div className="bg-blue-500/10 border border-blue-500/20 rounded p-4">
        <h3 className="font-semibold mb-2">✨ The Magic</h3>
        <p className="text-sm">
          When you navigate to <code className="bg-muted px-2 py-1 rounded">/plan/123</code>:
        </p>
        <ol className="list-decimal list-inside mt-2 space-y-1 text-sm">
          <li>createRouterFactory extracts <code className="bg-muted px-1 rounded">id: "123"</code></li>
          <li>withRelay loads the query with these variables</li>
          <li>Suspense shows loading state automatically</li>
          <li>Your component receives typed data prop!</li>
        </ol>
      </div>

      <div className="flex gap-2">
        <Button>No Boilerplate!</Button>
        <Button variant="outline">Fully Typed!</Button>
        <Button variant="secondary">Auto Loading!</Button>
      </div>
    </div>
  ),
};
