import { RouteDefinition } from "@/Router/withRelay";
import Query, { type PaidQuery } from "./__generated__/PaidQuery.graphql";
import PaidPage, { PaidQueryDef } from ".";
import { withAuthorization } from "@/AuthProvider";

export default {
  path: "/paid",
  component: withAuthorization(PaidPage, ["client"]),
  gqlQuery: PaidQueryDef,
  query: Query,
} satisfies RouteDefinition<PaidQuery>;
