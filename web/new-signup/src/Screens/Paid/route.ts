import { RouteDefinition } from "@/Router/withRelay";
import Query, { type PaidQuery } from "./__generated__/PaidQuery.graphql";
import { PaidQueryDef } from "./Paid";
import { withAuthorization } from "@/AuthProvider";
import React from "react";

export default {
  path: "/paid",
  component: withAuthorization(React.lazy(() => import(".")), ["client"]),
  gqlQuery: PaidQueryDef,
  query: Query,
} satisfies RouteDefinition<PaidQuery>;
