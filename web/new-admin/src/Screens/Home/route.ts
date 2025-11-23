import { RouteDefinition } from "@/Router/withRelay";
import Query, { type HomeQuery } from "./__generated__/HomeQuery.graphql";
import { HomeQueryDef } from "./Home";
import { withAuthorization } from "@/AuthProvider";
import React from "react";

export default {
  path: "/",
  component: withAuthorization(React.lazy(() => import("."))),
  gqlQuery: HomeQueryDef,
  query: Query,
} satisfies RouteDefinition<HomeQuery>;
