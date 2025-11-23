import { type RouteDefinition } from "@/Router/withRelay";
import Query, { type HomeQuery } from "./__generated__/HomeQuery.graphql";
import { HomeQueryDef } from "./Home";
import { withAuthorization } from "@/AuthProvider";
import { lazy } from "react";

export default {
  path: "/",
  component: withAuthorization(lazy(() => import("."))),
  gqlQuery: HomeQueryDef,
  query: Query,
} satisfies RouteDefinition<HomeQuery>;
