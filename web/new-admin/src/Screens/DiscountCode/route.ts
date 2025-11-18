import { RouteDefinition } from "@/Router/withRelay";
import Query, {
  type DiscountCodeQuery,
} from "./__generated__/DiscountCodeQuery.graphql";
import { DiscountCodeQueryDef } from "./DiscountCode";
import { withAuthorization } from "@/AuthProvider";
import React from "react";

export default {
  path: "/discount-code/:id?",
  component: withAuthorization(React.lazy(() => import("."))),
  gqlQuery: DiscountCodeQueryDef,
  query: Query,
} satisfies RouteDefinition<DiscountCodeQuery>;
