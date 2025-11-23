import { RouteDefinition } from "@/Router/withRelay";
import Query, {
  type DashboardQuery,
} from "./__generated__/DashboardQuery.graphql";
import { DashboardQueryDef } from "./Dashboard";
import { withAuthorization } from "@/AuthProvider";
import React from "react";

export default {
  path: "/dashboard",
  component: withAuthorization(
    React.lazy(() => import(".")),
    ["client"]
  ),
  gqlQuery: DashboardQueryDef,
  query: Query,
} satisfies RouteDefinition<DashboardQuery>;
