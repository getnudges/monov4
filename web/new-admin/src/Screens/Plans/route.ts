import { RouteDefinition } from "@/Router/withRelay";
import Query, { type PlansQuery } from "./__generated__/PlansQuery.graphql";
import { PlansQueryDef } from "./Plans";
import { withAuthorization } from "@/AuthProvider";
import React from "react";

export default {
  path: "/plans",
  component: withAuthorization(React.lazy(() => import("."))),
  gqlQuery: PlansQueryDef,
  query: Query,
} satisfies RouteDefinition<PlansQuery>;
