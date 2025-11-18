import { RouteDefinition } from "@/Router/withRelay";
import Query, { type PlanQuery } from "./__generated__/PlanQuery.graphql";
import { PlanQueryDef } from "./Plan";
import { withAuthorization } from "@/AuthProvider";
import React from "react";

export default {
  path: "/plan/:id?",
  component: withAuthorization(React.lazy(() => import("."))),
  gqlQuery: PlanQueryDef,
  query: Query,
} satisfies RouteDefinition<PlanQuery>;
