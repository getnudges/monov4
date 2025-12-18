import{j as e}from"./jsx-runtime-CgLq-oUW.js";import{useMDXComponents as i}from"./index-CX-RqswR.js";import{M as t}from"./index-C9Fh8Bod.js";import"./index-2peij01d.js";import"./iframe-B630B9b2.js";import"./index-NmXEX80k.js";import"./index-B37wPWDb.js";import"./index-DrFu-skq.js";function s(r){const n={code:"code",h1:"h1",h2:"h2",h3:"h3",hr:"hr",li:"li",ol:"ol",p:"p",pre:"pre",strong:"strong",ul:"ul",...i(),...r.components};return e.jsxs(e.Fragment,{children:[e.jsx(t,{title:"Introduction"}),`
`,e.jsx(n.h1,{id:"new-admin---relay-patterns-showcase",children:"New Admin - Relay Patterns Showcase"}),`
`,e.jsxs(n.p,{children:["Welcome to the ",e.jsx(n.strong,{children:"new-admin"})," component and pattern library! This Storybook demonstrates the custom Relay integration patterns that power the admin interface."]}),`
`,e.jsx(n.h2,{id:"what-makes-this-special",children:"What Makes This Special?"}),`
`,e.jsxs(n.p,{children:["This app features a ",e.jsx(n.strong,{children:"custom-built Relay + Wouter integration"})," that provides:"]}),`
`,e.jsxs(n.ul,{children:[`
`,e.jsxs(n.li,{children:["🔄 ",e.jsx(n.strong,{children:"Automatic query variable extraction"})," from URL params and query strings"]}),`
`,e.jsxs(n.li,{children:["⚡ ",e.jsx(n.strong,{children:"Type-safe GraphQL operations"})," with generated TypeScript types"]}),`
`,e.jsxs(n.li,{children:["🔌 ",e.jsx(n.strong,{children:"Real-time subscriptions"})," via WebSocket"]}),`
`,e.jsxs(n.li,{children:["🎯 ",e.jsx(n.strong,{children:"Route-driven data loading"})," with built-in suspense"]}),`
`,e.jsxs(n.li,{children:["🔁 ",e.jsx(n.strong,{children:"Query refresh capability"})," via context hooks"]}),`
`]}),`
`,e.jsx(n.h2,{id:"key-patterns",children:"Key Patterns"}),`
`,e.jsx(n.h3,{id:"1-route-driven-data-loading",children:"1. Route-Driven Data Loading"}),`
`,e.jsx(n.p,{children:"Routes declare their data requirements via GraphQL queries. The router automatically:"}),`
`,e.jsxs(n.ul,{children:[`
`,e.jsx(n.li,{children:"Extracts URL params as query variables"}),`
`,e.jsx(n.li,{children:"Loads the query when the route is visited"}),`
`,e.jsx(n.li,{children:"Shows loading states via Suspense"}),`
`,e.jsx(n.li,{children:"Provides refresh capability"}),`
`]}),`
`,e.jsx(n.p,{children:e.jsx(n.strong,{children:"Example:"})}),`
`,e.jsx(n.pre,{children:e.jsx(n.code,{className:"language-typescript",children:`// route.ts\r
export default {\r
  path: '/plan/:id',\r
  query: PlanQuery,\r
  gqlQuery: PlanQueryDef,\r
  component: PlanScreen,\r
  fetchPolicy: 'store-or-network'\r
};
`})}),`
`,e.jsxs(n.p,{children:["The ",e.jsx(n.code,{children:"id"})," param automatically becomes ",e.jsx(n.code,{children:'{ id: "..." }'})," variables for the GraphQL query!"]}),`
`,e.jsxs(n.h3,{id:"2-custom-relay-hoc-withrelay",children:["2. Custom Relay HOC (",e.jsx(n.code,{children:"withRelay"}),")"]}),`
`,e.jsx(n.p,{children:"A higher-order component that wraps the router to provide Relay integration:"}),`
`,e.jsxs(n.ul,{children:[`
`,e.jsx(n.li,{children:"Manages query loading lifecycle"}),`
`,e.jsx(n.li,{children:"Provides Suspense boundaries"}),`
`,e.jsxs(n.li,{children:["Exposes ",e.jsx(n.code,{children:"useRelayScreenContext()"})," for refresh"]}),`
`]}),`
`,e.jsxs(n.h3,{id:"3-router-factory-createrouterfactory",children:["3. Router Factory (",e.jsx(n.code,{children:"createRouterFactory"}),")"]}),`
`,e.jsx(n.p,{children:"Bridges wouter with Relay by:"}),`
`,e.jsxs(n.ul,{children:[`
`,e.jsx(n.li,{children:"Extracting route params from URL"}),`
`,e.jsx(n.li,{children:"Converting params to GraphQL variables"}),`
`,e.jsx(n.li,{children:"Wrapping screens in error boundaries"}),`
`]}),`
`,e.jsx(n.h3,{id:"4-real-time-subscriptions",children:"4. Real-time Subscriptions"}),`
`,e.jsx(n.p,{children:"GraphQL subscriptions for live updates:"}),`
`,e.jsxs(n.ul,{children:[`
`,e.jsxs(n.li,{children:["WebSocket connection via ",e.jsx(n.code,{children:"graphql-ws"})]}),`
`,e.jsx(n.li,{children:"Auto-updates Relay cache"}),`
`,e.jsxs(n.li,{children:["Form sync with ",e.jsx(n.code,{children:"keepDirtyValues"})]}),`
`]}),`
`,e.jsx(n.h2,{id:"component-library",children:"Component Library"}),`
`,e.jsx(n.p,{children:"This Storybook includes:"}),`
`,e.jsx(n.h3,{id:"components",children:"Components"}),`
`,e.jsxs(n.ul,{children:[`
`,e.jsxs(n.li,{children:[e.jsx(n.strong,{children:"shadcn/ui primitives"})," - Button, Card, Form, Input, etc."]}),`
`,e.jsxs(n.li,{children:[e.jsx(n.strong,{children:"Custom components"})," - NavHeader, Snackbar, ErrorDialog"]}),`
`,e.jsxs(n.li,{children:[e.jsx(n.strong,{children:"Theme support"})," - Dark/light mode with CSS variables"]}),`
`]}),`
`,e.jsx(n.h3,{id:"patterns",children:"Patterns"}),`
`,e.jsxs(n.ul,{children:[`
`,e.jsxs(n.li,{children:[e.jsx(n.strong,{children:"Relay Screen Pattern"})," - How to build a screen with Relay"]}),`
`,e.jsxs(n.li,{children:[e.jsx(n.strong,{children:"Form + Mutation"})," - React Hook Form + Zod + Relay mutations"]}),`
`,e.jsxs(n.li,{children:[e.jsx(n.strong,{children:"Subscriptions"})," - Real-time updates pattern"]}),`
`,e.jsxs(n.li,{children:[e.jsx(n.strong,{children:"Auth Protection"})," - ",e.jsx(n.code,{children:"withAuthorization"})," HOC"]}),`
`]}),`
`,e.jsx(n.h2,{id:"tech-stack",children:"Tech Stack"}),`
`,e.jsxs(n.ul,{children:[`
`,e.jsxs(n.li,{children:[e.jsx(n.strong,{children:"React 18"})," + TypeScript"]}),`
`,e.jsxs(n.li,{children:[e.jsx(n.strong,{children:"Vite"})," for build tooling"]}),`
`,e.jsxs(n.li,{children:[e.jsx(n.strong,{children:"Relay"})," for GraphQL client"]}),`
`,e.jsxs(n.li,{children:[e.jsx(n.strong,{children:"Wouter"})," for routing"]}),`
`,e.jsxs(n.li,{children:[e.jsx(n.strong,{children:"shadcn/ui"})," for components"]}),`
`,e.jsxs(n.li,{children:[e.jsx(n.strong,{children:"Tailwind CSS"})," for styling"]}),`
`,e.jsxs(n.li,{children:[e.jsx(n.strong,{children:"React Hook Form"})," + Zod for forms"]}),`
`]}),`
`,e.jsx(n.h2,{id:"explore-the-patterns",children:"Explore the Patterns"}),`
`,e.jsx(n.p,{children:"Navigate through the sidebar to see:"}),`
`,e.jsxs(n.ol,{children:[`
`,e.jsxs(n.li,{children:[e.jsx(n.strong,{children:"Components"})," - UI building blocks"]}),`
`,e.jsxs(n.li,{children:[e.jsx(n.strong,{children:"Patterns"})," - The custom Relay patterns (the cool stuff!)"]}),`
`]}),`
`,e.jsx(n.hr,{}),`
`,e.jsx(n.p,{children:"Built with ❤️ to showcase modern React + GraphQL patterns."})]})}function p(r={}){const{wrapper:n}={...i(),...r.components};return n?e.jsx(n,{...r,children:e.jsx(s,{...r})}):s(r)}export{p as default};
