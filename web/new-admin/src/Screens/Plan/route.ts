import { RouteDefinition } from "@/Router/withRelay";
import Query, { type PlanQuery } from "./__generated__/PlanQuery.graphql";
import PlanPage, { PlanQueryDef } from ".";
import { withAuthorization } from "@/AuthProvider";

export default {
  path: "/plan/:id?",
  component: withAuthorization(PlanPage),
  gqlQuery: PlanQueryDef,
  query: Query,
} satisfies RouteDefinition<PlanQuery>;
