import { type RouteDefinition } from "@/Router/withRelay";
import Query, { type PlanQuery } from "./__generated__/PlanQuery.graphql";
import { PlanQueryDef } from "./PlanQuery";
import { withAuthorization } from "@/AuthProvider";
import { lazy } from "react";

export default {
  path: "/plan/:id?",
  component: withAuthorization(lazy(() => import("."))),
  gqlQuery: PlanQueryDef,
  query: Query,
} satisfies RouteDefinition<PlanQuery>;
