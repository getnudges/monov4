import { RouteDefinition } from "@/Router/withRelay";
import Query, { type PortalQuery } from "./__generated__/PortalQuery.graphql";
import { PortalQueryDef } from "./Portal";
import React from "react";

export default {
  path: "/portal",
  component: React.lazy(() => import(".")),
  gqlQuery: PortalQueryDef,
  query: Query,
} satisfies RouteDefinition<PortalQuery>;
