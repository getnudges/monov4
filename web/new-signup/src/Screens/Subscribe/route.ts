import Query from "./__generated__/SubscribeQuery.graphql";
import { SubscribeQueryDef } from "./Subscribe";
import React from "react";

export default {
  path: "/subscribe/:slug",
  component: React.lazy(() => import(".")),
  gqlQuery: SubscribeQueryDef,
  query: Query,
};
