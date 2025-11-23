import Query, { type PlansQuery } from "./__generated__/PlansQuery.graphql";
import type { RouteDefinition } from "../../Router/withRelay";
import { PlansQueryDef } from "./Plans";
import { withAuthorization } from "@/AuthProvider";
import React from "react";

export default {
  path: "/plans",
  component: withAuthorization(
    React.lazy(() => import(".")),
    ["client"]
  ),
  gqlQuery: PlansQueryDef,
  query: Query,
} satisfies RouteDefinition<PlansQuery>;
