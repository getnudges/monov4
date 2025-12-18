import type { Meta, StoryObj } from "@storybook/react";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { useState } from "react";
import { Badge } from "@/components/ui/badge";

// Simulates a component that uses subscriptions
function SubscriptionDemo() {
  const [updates, setUpdates] = useState<Array<{ time: string; message: string }>>([]);
  const [subscribed, setSubscribed] = useState(false);

  const simulateUpdate = () => {
    const newUpdate = {
      time: new Date().toLocaleTimeString(),
      message: `Plan updated: foreignServiceId = "price_${Math.random().toString(36).substring(7)}"`,
    };
    setUpdates((prev) => [newUpdate, ...prev].slice(0, 10));
  };

  return (
    <div className="max-w-2xl mx-auto p-8 space-y-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center justify-between">
            Real-time Subscription Pattern
            {subscribed && (
              <Badge variant="default" className="bg-green-500">
                ● Subscribed
              </Badge>
            )}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="bg-muted p-4 rounded">
            <p className="text-sm">
              When a plan is updated via Stripe webhook, the GraphQL gateway pushes updates via
              WebSocket subscription. The UI automatically reflects the changes!
            </p>
          </div>

          <div className="flex gap-2">
            <Button
              onClick={() => {
                setSubscribed(true);
                setTimeout(simulateUpdate, 500);
              }}
              disabled={subscribed}
            >
              Subscribe to Updates
            </Button>
            <Button onClick={simulateUpdate} variant="outline" disabled={!subscribed}>
              Simulate Stripe Webhook
            </Button>
          </div>

          {updates.length > 0 && (
            <div className="space-y-2">
              <h3 className="font-medium text-sm">Update Log</h3>
              <div className="space-y-1 max-h-64 overflow-y-auto">
                {updates.map((update, i) => (
                  <div
                    key={i}
                    className="text-xs bg-green-500/10 border border-green-500/20 rounded p-2"
                  >
                    <span className="text-muted-foreground">[{update.time}]</span> {update.message}
                  </div>
                ))}
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>How It Works</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <h4 className="font-medium text-sm">1. Define Subscription</h4>
            <pre className="bg-muted p-3 rounded text-xs overflow-x-auto">
              {`const PlanUpdatedSub = graphql\`
  subscription PlanUpdatedSubscription($id: ID!) {
    onPlanUpdated(id: $id) {
      id
      foreignServiceId
      name
    }
  }
\`;`}
            </pre>
          </div>

          <div className="space-y-2">
            <h4 className="font-medium text-sm">2. Use in Component</h4>
            <pre className="bg-muted p-3 rounded text-xs overflow-x-auto">
              {`usePlanUpdatedSubscription(planId, (errors) => {
  // Handle subscription errors
  if (errors.length) {
    showErrorDialog(errors);
  }
});`}
            </pre>
          </div>

          <div className="space-y-2">
            <h4 className="font-medium text-sm">3. Automatic Cache Update</h4>
            <pre className="bg-muted p-3 rounded text-xs overflow-x-auto">
              {`// In PlanEditor.tsx
useEffect(() => {
  // When subscription updates the cache,
  // reset form with new data
  form.reset(createFormData(data), {
    keepDirtyValues: true  // Preserve unsaved edits!
  });
}, [data, form]);`}
            </pre>
          </div>

          <div className="bg-blue-500/10 border border-blue-500/20 rounded p-4 mt-4">
            <h4 className="font-semibold mb-2 text-sm">The Flow</h4>
            <ol className="list-decimal list-inside space-y-1 text-xs">
              <li>User creates plan in UI</li>
              <li>Plan synced to Stripe (via Kafka)</li>
              <li>Stripe webhook fires → Webhooks service</li>
              <li>Webhooks → Kafka → ProductApi</li>
              <li>ProductApi updates plan with foreignServiceId</li>
              <li>GraphQL Gateway fires onPlanUpdated subscription</li>
              <li>WebSocket pushes update to UI</li>
              <li>Relay updates cache automatically</li>
              <li>Component re-renders with new data!</li>
            </ol>
          </div>

          <div className="bg-green-500/10 border border-green-500/20 rounded p-4">
            <h4 className="font-semibold mb-2 text-sm">✨ Benefits</h4>
            <ul className="space-y-1 text-xs">
              <li>✓ No polling needed</li>
              <li>✓ Instant updates from external events</li>
              <li>✓ Relay cache automatically updated</li>
              <li>✓ Form preserves unsaved changes (keepDirtyValues)</li>
              <li>✓ Works across multiple browser tabs!</li>
            </ul>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

const meta = {
  title: "Patterns/Real-time Subscriptions",
  component: SubscriptionDemo,
  parameters: {
    layout: "fullscreen",
  },
  tags: ["autodocs"],
} satisfies Meta<typeof SubscriptionDemo>;

export default meta;
type Story = StoryObj<typeof meta>;

export const InteractiveDemo: Story = {};

export const CodeExample = {
  render: () => (
    <div className="max-w-4xl mx-auto p-8 space-y-6">
      <h1 className="text-3xl font-bold">Real-time Subscription Code</h1>

      <Card>
        <CardHeader>
          <CardTitle>Custom Hook: usePlanUpdatedSubscription</CardTitle>
        </CardHeader>
        <CardContent>
          <pre className="bg-muted p-4 rounded text-sm overflow-x-auto">
            {`// hooks/PlanUpdated.ts
import { graphql, useSubscription } from "react-relay";

export function usePlanUpdatedSubscription(
  planId: string,
  onError: (errors: GraphQLSubscriptionError[]) => void
) {
  useSubscription({
    subscription: graphql\`
      subscription PlanUpdatedSubscription($id: ID!) {
        onPlanUpdated(id: $id) {
          id
          name
          foreignServiceId
          ...PlanEditor_plan
        }
      }
    \`,
    variables: { id: planId },
    onError: (error) => onError([error])
  });
}`}
          </pre>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Usage in PlanEditor</CardTitle>
        </CardHeader>
        <CardContent>
          <pre className="bg-muted p-4 rounded text-sm overflow-x-auto">
            {`// PlanEditor.tsx
const EditPlanForm = ({ id, ...props }) => {
  const [errors, setErrors] = useState([]);

  // Subscribe to plan updates
  usePlanUpdatedSubscription(id, setErrors);

  return (
    <>
      <PlanForm {...props} />
      {errors.length > 0 && (
        <ErrorDialog
          error={errors[0]}
          onClose={() => setErrors([])}
        />
      )}
    </>
  );
};`}
          </pre>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Form Sync with Subscription Updates</CardTitle>
        </CardHeader>
        <CardContent>
          <pre className="bg-muted p-4 rounded text-sm overflow-x-auto">
            {`// When subscription updates Relay cache, sync form
useEffect(() => {
  form.reset(createFormData(data), {
    keepDirtyValues: true  // Don't lose user's unsaved edits!
  });
}, [data, form]);`}
          </pre>
        </CardContent>
      </Card>

      <div className="bg-yellow-500/10 border border-yellow-500/20 rounded p-4">
        <h3 className="font-semibold mb-2">⚠️ Important: keepDirtyValues</h3>
        <p className="text-sm">
          When the subscription updates the form data, we use <code>keepDirtyValues: true</code> to
          preserve any unsaved changes the user has made. This prevents data loss while still
          reflecting external updates (like Stripe IDs).
        </p>
      </div>
    </div>
  ),
};
