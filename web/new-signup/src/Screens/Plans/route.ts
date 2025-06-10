import Query, { type PlansQuery } from "./__generated__/PlansQuery.graphql";
import type { RouteDefinition } from "../../Router/withRelay";
import PlansPage, { PlansQueryDef } from ".";
import { withAuthorization } from "@/AuthProvider";

export default {
  path: "/plans",
  component: withAuthorization(PlansPage, ["client"]),
  gqlQuery: PlansQueryDef,
  query: Query,
} satisfies RouteDefinition<PlansQuery>;
