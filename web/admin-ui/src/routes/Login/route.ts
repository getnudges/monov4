import { type RouteDefinition } from "@/Router/withRelay";
import Query, { type LoginQuery } from "./__generated__/LoginQuery.graphql";
import { LoginQueryDef } from "./Login";
import { lazy } from "react";

export default {
  path: "/login",
  component: lazy(() => import(".")),
  gqlQuery: LoginQueryDef,
  query: Query,
} satisfies RouteDefinition<LoginQuery>;
