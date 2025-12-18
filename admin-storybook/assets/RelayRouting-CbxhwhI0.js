import{j as e}from"./jsx-runtime-CgLq-oUW.js";import{useMDXComponents as a}from"./index-CX-RqswR.js";import{M as s}from"./index-C9Fh8Bod.js";import"./index-2peij01d.js";import"./iframe-B630B9b2.js";import"./index-NmXEX80k.js";import"./index-B37wPWDb.js";import"./index-DrFu-skq.js";function t(n){const r={code:"code",h1:"h1",h2:"h2",h3:"h3",hr:"hr",li:"li",ol:"ol",p:"p",pre:"pre",strong:"strong",ul:"ul",...a(),...n.components};return e.jsxs(e.Fragment,{children:[e.jsx(s,{title:"Patterns/Relay Routing"}),`
`,e.jsx(r.h1,{id:"relay-routing-pattern",children:"Relay Routing Pattern"}),`
`,e.jsxs(r.p,{children:["This app uses a ",e.jsx(r.strong,{children:"custom-built integration"})," between Relay and Wouter that provides automatic data loading based on route parameters."]}),`
`,e.jsx(r.h2,{id:"the-problem",children:"The Problem"}),`
`,e.jsx(r.p,{children:"Standard React routing requires:"}),`
`,e.jsxs(r.ol,{children:[`
`,e.jsx(r.li,{children:"Manually extracting URL params"}),`
`,e.jsx(r.li,{children:"Passing params to GraphQL queries"}),`
`,e.jsx(r.li,{children:"Managing loading states"}),`
`,e.jsx(r.li,{children:"Handling errors"}),`
`]}),`
`,e.jsx(r.p,{children:"This creates boilerplate in every screen component."}),`
`,e.jsx(r.h2,{id:"the-solution",children:"The Solution"}),`
`,e.jsxs(r.p,{children:["Our custom ",e.jsx(r.code,{children:"withRelay"})," HOC + ",e.jsx(r.code,{children:"createRouterFactory"})," pattern automates all of this!"]}),`
`,e.jsx(r.h2,{id:"how-it-works",children:"How It Works"}),`
`,e.jsx(r.h3,{id:"1-define-a-route",children:"1. Define a Route"}),`
`,e.jsx(r.pre,{children:e.jsx(r.code,{className:"language-typescript",children:`// Screens/Plan/route.ts\r
import { RouteDefinition } from "@/Router/withRelay";\r
import Query, { type PlanQuery } from "./__generated__/PlanQuery.graphql";\r
import { PlanQueryDef } from "./Plan";\r
\r
export default {\r
  path: "/plan/:id",          // wouter path pattern\r
  query: Query,               // Relay compiled query\r
  gqlQuery: PlanQueryDef,     // GraphQL query definition\r
  component: PlanScreen,      // Your component\r
  fetchPolicy: 'store-or-network'\r
} satisfies RouteDefinition<PlanQuery>;
`})}),`
`,e.jsx(r.h3,{id:"2-define-graphql-query",children:"2. Define GraphQL Query"}),`
`,e.jsx(r.pre,{children:e.jsx(r.code,{className:"language-typescript",children:`// Screens/Plan/Plan.ts\r
import { graphql } from "react-relay";\r
\r
export const PlanQueryDef = graphql\\\`\r
  query PlanQuery($id: ID!) {\r
    plan(id: $id) {\r
      id\r
      name\r
      description\r
    }\r
  }\r
\\\`;
`})}),`
`,e.jsx(r.h3,{id:"3-component-receives-data",children:"3. Component Receives Data"}),`
`,e.jsx(r.pre,{children:e.jsx(r.code,{className:"language-typescript",children:`// Screens/Plan/index.tsx\r
import { RelayRoute } from "@/Router/withRelay";\r
import type { PlanQuery } from "./__generated__/PlanQuery.graphql";\r
\r
export default function PlanScreen({\r
  data\r
}: Readonly<RelayRoute<PlanQuery>>) {\r
  // data is fully typed and loaded!\r
  return <h1>{data.plan?.name}</h1>;\r
}
`})}),`
`,e.jsx(r.h2,{id:"magic-happening-behind-the-scenes",children:"Magic Happening Behind the Scenes"}),`
`,e.jsx(r.h3,{id:"url-param-extraction",children:"URL Param Extraction"}),`
`,e.jsxs(r.p,{children:["When you navigate to ",e.jsx(r.code,{children:"/plan/abc123"}),":"]}),`
`,e.jsxs(r.ol,{children:[`
`,e.jsxs(r.li,{children:[e.jsx(r.strong,{children:"createRouterFactory"})," extracts params: ",e.jsx(r.code,{children:'{ id: "abc123" }'})]}),`
`,e.jsxs(r.li,{children:[e.jsx(r.strong,{children:"withRelay"})," passes these as GraphQL variables"]}),`
`,e.jsxs(r.li,{children:[e.jsx(r.strong,{children:"Relay"})," loads the query automatically"]}),`
`,e.jsxs(r.li,{children:[e.jsx(r.strong,{children:"React Suspense"})," shows loading state"]}),`
`,e.jsxs(r.li,{children:[e.jsx(r.strong,{children:"Component"})," receives typed data"]}),`
`]}),`
`,e.jsx(r.h3,{id:"query-string-support",children:"Query String Support"}),`
`,e.jsx(r.p,{children:"You can also extract query strings as variables:"}),`
`,e.jsx(r.pre,{children:e.jsx(r.code,{className:"language-typescript",children:`const router = withRelay(\r
  createRouterFactory(true), // includeQueryString: true\r
  routes,\r
  LoadingScreen\r
);
`})}),`
`,e.jsxs(r.p,{children:["Now ",e.jsx(r.code,{children:"/plan/abc123?debug=true"})," gives you ",e.jsx(r.code,{children:'{ id: "abc123", debug: "true" }'})," as variables!"]}),`
`,e.jsx(r.h2,{id:"type-safety",children:"Type Safety"}),`
`,e.jsx(r.p,{children:"Everything is fully typed:"}),`
`,e.jsx(r.pre,{children:e.jsx(r.code,{className:"language-typescript",children:`// GraphQL query generates this type automatically\r
type PlanQuery = {\r
  response: {\r
    plan: {\r
      id: string;\r
      name: string;\r
      description: string;\r
    } | null;\r
  };\r
  variables: {\r
    id: string;\r
  };\r
};\r
\r
// Your component gets typed props\r
function PlanScreen({ data }: Readonly<RelayRoute<PlanQuery>>) {\r
  // TypeScript knows data.plan has id, name, description\r
  // Autocomplete works perfectly!\r
}
`})}),`
`,e.jsx(r.h2,{id:"refresh-capability",children:"Refresh Capability"}),`
`,e.jsx(r.p,{children:"Need to refetch the query? Use the hook:"}),`
`,e.jsx(r.pre,{children:e.jsx(r.code,{className:"language-typescript",children:`import { useRelayScreenContext } from "@/Router/withRelay";\r
\r
function MyComponent() {\r
  const { refresh, variables } = useRelayScreenContext<PlanQuery>();\r
\r
  const handleRefresh = () => {\r
    refresh(variables); // Re-fetch with same variables\r
    // Or: refresh({ id: 'new-id' }); // Different variables\r
  };\r
\r
  return <button onClick={handleRefresh}>Refresh</button>;\r
}
`})}),`
`,e.jsx(r.h2,{id:"loading-states",children:"Loading States"}),`
`,e.jsx(r.p,{children:"Handled automatically via React Suspense:"}),`
`,e.jsx(r.pre,{children:e.jsx(r.code,{className:"language-typescript",children:`// In withRelay setup\r
const router = withRelay(\r
  createRouterFactory(true),\r
  routes,\r
  LoadingScreen  // Shows while query loads\r
);\r
\r
// Each route can also have custom skeleton\r
export default {\r
  path: "/plan/:id",\r
  query: PlanQuery,\r
  gqlQuery: PlanQueryDef,\r
  component: PlanScreen,\r
  skeleton: <PlanSkeleton />  // Custom loading UI\r
};
`})}),`
`,e.jsx(r.h2,{id:"error-handling",children:"Error Handling"}),`
`,e.jsx(r.p,{children:"Errors are caught by ErrorBoundary wrapper:"}),`
`,e.jsx(r.pre,{children:e.jsx(r.code,{className:"language-typescript",children:`// In createRouterFactory.tsx\r
return (\r
  <ErrorBoundary>\r
    <Component queryVars={queryVars} {...props} />\r
  </ErrorBoundary>\r
);
`})}),`
`,e.jsx(r.h2,{id:"the-architecture",children:"The Architecture"}),`
`,e.jsx(r.pre,{children:e.jsx(r.code,{children:`User navigates to /plan/123\r
        ↓\r
createRouterFactory extracts { id: "123" }\r
        ↓\r
withRelay wraps component with RelayScreenWrapper\r
        ↓\r
useQueryLoader(query, { id: "123" })\r
        ↓\r
React Suspense (shows LoadingScreen)\r
        ↓\r
Query completes\r
        ↓\r
Component renders with data prop
`})}),`
`,e.jsx(r.h2,{id:"benefits",children:"Benefits"}),`
`,e.jsxs(r.p,{children:["✅ ",e.jsx(r.strong,{children:"Zero boilerplate"}),` - No manual param extraction\r
✅ `,e.jsx(r.strong,{children:"Type-safe"}),` - Generated types for all queries\r
✅ `,e.jsx(r.strong,{children:"Automatic loading"}),` - Suspense handles loading states\r
✅ `,e.jsx(r.strong,{children:"Error boundaries"}),` - Built-in error handling\r
✅ `,e.jsx(r.strong,{children:"Refresh capability"}),` - Easy query refetch\r
✅ `,e.jsx(r.strong,{children:"Developer experience"})," - Just define route + query + component!"]}),`
`,e.jsx(r.h2,{id:"comparison",children:"Comparison"}),`
`,e.jsx(r.h3,{id:"without-this-pattern",children:"Without This Pattern"}),`
`,e.jsx(r.pre,{children:e.jsx(r.code,{className:"language-typescript",children:`function PlanScreen() {\r
  const params = useParams();\r
  const [data, setData] = useState(null);\r
  const [loading, setLoading] = useState(true);\r
  const [error, setError] = useState(null);\r
\r
  useEffect(() => {\r
    setLoading(true);\r
    fetchQuery(PlanQuery, { id: params.id })\r
      .then(setData)\r
      .catch(setError)\r
      .finally(() => setLoading(false));\r
  }, [params.id]);\r
\r
  if (loading) return <Loading />;\r
  if (error) return <Error error={error} />;\r
\r
  return <h1>{data.plan.name}</h1>;\r
}
`})}),`
`,e.jsx(r.h3,{id:"with-this-pattern",children:"With This Pattern"}),`
`,e.jsx(r.pre,{children:e.jsx(r.code,{className:"language-typescript",children:`export default function PlanScreen({ data }: Readonly<RelayRoute<PlanQuery>>) {\r
  return <h1>{data.plan?.name}</h1>;\r
}
`})}),`
`,e.jsxs(r.p,{children:[e.jsx(r.strong,{children:"That's it!"})," The pattern handles everything else."]}),`
`,e.jsx(r.hr,{}),`
`,e.jsx(r.p,{children:"This pattern is the heart of what makes this app special. It combines the best of:"}),`
`,e.jsxs(r.ul,{children:[`
`,e.jsx(r.li,{children:"Relay's normalized cache and type generation"}),`
`,e.jsx(r.li,{children:"Wouter's lightweight routing"}),`
`,e.jsx(r.li,{children:"React Suspense for loading states"}),`
`,e.jsx(r.li,{children:"TypeScript for safety"}),`
`]}),`
`,e.jsx(r.p,{children:"The result: A routing system that feels magical but is actually simple, testable, and maintainable."})]})}function y(n={}){const{wrapper:r}={...a(),...n.components};return r?e.jsx(r,{...n,children:e.jsx(t,{...n})}):t(n)}export{y as default};
