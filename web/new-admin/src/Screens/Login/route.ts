import { RouteDefinition } from "@/Router/withRelay";
import Query, { type LoginQuery } from "./__generated__/LoginQuery.graphql";
import { LoginQueryDef } from "./Login";
import React from "react";

export default {
  path: "/login",
  component: React.lazy(() => import(".")),
  gqlQuery: LoginQueryDef,
  query: Query,
} satisfies RouteDefinition<LoginQuery>;
