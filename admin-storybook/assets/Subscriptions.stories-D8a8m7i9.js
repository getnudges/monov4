import{j as e}from"./jsx-runtime-CgLq-oUW.js";import{C as s,a,b as t,c as n}from"./card-C6gNvGae.js";import{c as C,a as N,B as m}from"./button-R1TzV71x.js";import{r as h}from"./index-2peij01d.js";const w=N("inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2",{variants:{variant:{default:"border-transparent bg-primary text-primary-foreground hover:bg-primary/80",secondary:"border-transparent bg-secondary text-secondary-foreground hover:bg-secondary/80",destructive:"border-transparent bg-destructive text-destructive-foreground hover:bg-destructive/80",outline:"text-foreground"}},defaultVariants:{variant:"default"}});function y({className:o,variant:c,...r}){return e.jsx("div",{className:C(w({variant:c}),o),...r})}y.__docgenInfo={description:"",methods:[],displayName:"Badge",composes:["VariantProps"]};function P(){const[o,c]=h.useState([]),[r,S]=h.useState(!1),u=()=>{const d={time:new Date().toLocaleTimeString(),message:`Plan updated: foreignServiceId = "price_${Math.random().toString(36).substring(7)}"`};c(p=>[d,...p].slice(0,10))};return e.jsxs("div",{className:"max-w-2xl mx-auto p-8 space-y-6",children:[e.jsxs(s,{children:[e.jsx(a,{children:e.jsxs(t,{className:"flex items-center justify-between",children:["Real-time Subscription Pattern",r&&e.jsx(y,{variant:"default",className:"bg-green-500",children:"● Subscribed"})]})}),e.jsxs(n,{className:"space-y-4",children:[e.jsx("div",{className:"bg-muted p-4 rounded",children:e.jsx("p",{className:"text-sm",children:"When a plan is updated via Stripe webhook, the GraphQL gateway pushes updates via WebSocket subscription. The UI automatically reflects the changes!"})}),e.jsxs("div",{className:"flex gap-2",children:[e.jsx(m,{onClick:()=>{S(!0),setTimeout(u,500)},disabled:r,children:"Subscribe to Updates"}),e.jsx(m,{onClick:u,variant:"outline",disabled:!r,children:"Simulate Stripe Webhook"})]}),o.length>0&&e.jsxs("div",{className:"space-y-2",children:[e.jsx("h3",{className:"font-medium text-sm",children:"Update Log"}),e.jsx("div",{className:"space-y-1 max-h-64 overflow-y-auto",children:o.map((d,p)=>e.jsxs("div",{className:"text-xs bg-green-500/10 border border-green-500/20 rounded p-2",children:[e.jsxs("span",{className:"text-muted-foreground",children:["[",d.time,"]"]})," ",d.message]},p))})]})]})]}),e.jsxs(s,{children:[e.jsx(a,{children:e.jsx(t,{children:"How It Works"})}),e.jsxs(n,{className:"space-y-4",children:[e.jsxs("div",{className:"space-y-2",children:[e.jsx("h4",{className:"font-medium text-sm",children:"1. Define Subscription"}),e.jsx("pre",{className:"bg-muted p-3 rounded text-xs overflow-x-auto",children:`const PlanUpdatedSub = graphql\`
  subscription PlanUpdatedSubscription($id: ID!) {
    onPlanUpdated(id: $id) {
      id
      foreignServiceId
      name
    }
  }
\`;`})]}),e.jsxs("div",{className:"space-y-2",children:[e.jsx("h4",{className:"font-medium text-sm",children:"2. Use in Component"}),e.jsx("pre",{className:"bg-muted p-3 rounded text-xs overflow-x-auto",children:`usePlanUpdatedSubscription(planId, (errors) => {
  // Handle subscription errors
  if (errors.length) {
    showErrorDialog(errors);
  }
});`})]}),e.jsxs("div",{className:"space-y-2",children:[e.jsx("h4",{className:"font-medium text-sm",children:"3. Automatic Cache Update"}),e.jsx("pre",{className:"bg-muted p-3 rounded text-xs overflow-x-auto",children:`// In PlanEditor.tsx
useEffect(() => {
  // When subscription updates the cache,
  // reset form with new data
  form.reset(createFormData(data), {
    keepDirtyValues: true  // Preserve unsaved edits!
  });
}, [data, form]);`})]}),e.jsxs("div",{className:"bg-blue-500/10 border border-blue-500/20 rounded p-4 mt-4",children:[e.jsx("h4",{className:"font-semibold mb-2 text-sm",children:"The Flow"}),e.jsxs("ol",{className:"list-decimal list-inside space-y-1 text-xs",children:[e.jsx("li",{children:"User creates plan in UI"}),e.jsx("li",{children:"Plan synced to Stripe (via Kafka)"}),e.jsx("li",{children:"Stripe webhook fires → Webhooks service"}),e.jsx("li",{children:"Webhooks → Kafka → ProductApi"}),e.jsx("li",{children:"ProductApi updates plan with foreignServiceId"}),e.jsx("li",{children:"GraphQL Gateway fires onPlanUpdated subscription"}),e.jsx("li",{children:"WebSocket pushes update to UI"}),e.jsx("li",{children:"Relay updates cache automatically"}),e.jsx("li",{children:"Component re-renders with new data!"})]})]}),e.jsxs("div",{className:"bg-green-500/10 border border-green-500/20 rounded p-4",children:[e.jsx("h4",{className:"font-semibold mb-2 text-sm",children:"✨ Benefits"}),e.jsxs("ul",{className:"space-y-1 text-xs",children:[e.jsx("li",{children:"✓ No polling needed"}),e.jsx("li",{children:"✓ Instant updates from external events"}),e.jsx("li",{children:"✓ Relay cache automatically updated"}),e.jsx("li",{children:"✓ Form preserves unsaved changes (keepDirtyValues)"}),e.jsx("li",{children:"✓ Works across multiple browser tabs!"})]})]})]})]})]})}const I={title:"Patterns/Real-time Subscriptions",component:P,parameters:{layout:"fullscreen"},tags:["autodocs"]},i={},l={render:()=>e.jsxs("div",{className:"max-w-4xl mx-auto p-8 space-y-6",children:[e.jsx("h1",{className:"text-3xl font-bold",children:"Real-time Subscription Code"}),e.jsxs(s,{children:[e.jsx(a,{children:e.jsx(t,{children:"Custom Hook: usePlanUpdatedSubscription"})}),e.jsx(n,{children:e.jsx("pre",{className:"bg-muted p-4 rounded text-sm overflow-x-auto",children:`// hooks/PlanUpdated.ts
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
}`})})]}),e.jsxs(s,{children:[e.jsx(a,{children:e.jsx(t,{children:"Usage in PlanEditor"})}),e.jsx(n,{children:e.jsx("pre",{className:"bg-muted p-4 rounded text-sm overflow-x-auto",children:`// PlanEditor.tsx
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
};`})})]}),e.jsxs(s,{children:[e.jsx(a,{children:e.jsx(t,{children:"Form Sync with Subscription Updates"})}),e.jsx(n,{children:e.jsx("pre",{className:"bg-muted p-4 rounded text-sm overflow-x-auto",children:`// When subscription updates Relay cache, sync form
useEffect(() => {
  form.reset(createFormData(data), {
    keepDirtyValues: true  // Don't lose user's unsaved edits!
  });
}, [data, form]);`})})]}),e.jsxs("div",{className:"bg-yellow-500/10 border border-yellow-500/20 rounded p-4",children:[e.jsx("h3",{className:"font-semibold mb-2",children:"⚠️ Important: keepDirtyValues"}),e.jsxs("p",{className:"text-sm",children:["When the subscription updates the form data, we use ",e.jsx("code",{children:"keepDirtyValues: true"})," to preserve any unsaved changes the user has made. This prevents data loss while still reflecting external updates (like Stripe IDs)."]})]})]})};var x,b,f;i.parameters={...i.parameters,docs:{...(x=i.parameters)==null?void 0:x.docs,source:{originalSource:"{}",...(f=(b=i.parameters)==null?void 0:b.docs)==null?void 0:f.source}}};var g,j,v;l.parameters={...l.parameters,docs:{...(g=l.parameters)==null?void 0:g.docs,source:{originalSource:`{
  render: () => <div className="max-w-4xl mx-auto p-8 space-y-6">\r
      <h1 className="text-3xl font-bold">Real-time Subscription Code</h1>\r
\r
      <Card>\r
        <CardHeader>\r
          <CardTitle>Custom Hook: usePlanUpdatedSubscription</CardTitle>\r
        </CardHeader>\r
        <CardContent>\r
          <pre className="bg-muted p-4 rounded text-sm overflow-x-auto">\r
            {\`// hooks/PlanUpdated.ts
import { graphql, useSubscription } from "react-relay";

export function usePlanUpdatedSubscription(
  planId: string,
  onError: (errors: GraphQLSubscriptionError[]) => void
) {
  useSubscription({
    subscription: graphql\\\`
      subscription PlanUpdatedSubscription($id: ID!) {
        onPlanUpdated(id: $id) {
          id
          name
          foreignServiceId
          ...PlanEditor_plan
        }
      }
    \\\`,
    variables: { id: planId },
    onError: (error) => onError([error])
  });
}\`}\r
          </pre>\r
        </CardContent>\r
      </Card>\r
\r
      <Card>\r
        <CardHeader>\r
          <CardTitle>Usage in PlanEditor</CardTitle>\r
        </CardHeader>\r
        <CardContent>\r
          <pre className="bg-muted p-4 rounded text-sm overflow-x-auto">\r
            {\`// PlanEditor.tsx
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
};\`}\r
          </pre>\r
        </CardContent>\r
      </Card>\r
\r
      <Card>\r
        <CardHeader>\r
          <CardTitle>Form Sync with Subscription Updates</CardTitle>\r
        </CardHeader>\r
        <CardContent>\r
          <pre className="bg-muted p-4 rounded text-sm overflow-x-auto">\r
            {\`// When subscription updates Relay cache, sync form
useEffect(() => {
  form.reset(createFormData(data), {
    keepDirtyValues: true  // Don't lose user's unsaved edits!
  });
}, [data, form]);\`}\r
          </pre>\r
        </CardContent>\r
      </Card>\r
\r
      <div className="bg-yellow-500/10 border border-yellow-500/20 rounded p-4">\r
        <h3 className="font-semibold mb-2">⚠️ Important: keepDirtyValues</h3>\r
        <p className="text-sm">\r
          When the subscription updates the form data, we use <code>keepDirtyValues: true</code> to\r
          preserve any unsaved changes the user has made. This prevents data loss while still\r
          reflecting external updates (like Stripe IDs).\r
        </p>\r
      </div>\r
    </div>
}`,...(v=(j=l.parameters)==null?void 0:j.docs)==null?void 0:v.source}}};const T=["InteractiveDemo","CodeExample"];export{l as CodeExample,i as InteractiveDemo,T as __namedExportsOrder,I as default};
