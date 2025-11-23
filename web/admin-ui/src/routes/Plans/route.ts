import { type RouteDefinition } from "@/Router/withRelay";
import Query, { type PlansQuery } from "./__generated__/PlansQuery.graphql";
import { PlansQueryDef } from "./Plans";
import { withAuthorization } from "@/AuthProvider";
import { lazy } from "react";

export default {
  path: "/plans",
  component: withAuthorization(lazy(() => import("."))),
  gqlQuery: PlansQueryDef,
  query: Query,
} satisfies RouteDefinition<PlansQuery>;
