import { RouteDefinition } from "@/Router/withRelay";
import Query, { type PlansQuery } from "./__generated__/PlansQuery.graphql";
import PlansPage, { PlansQueryDef } from ".";
import { withAuthorization } from "@/AuthProvider";

export default {
  path: "/plans",
  component: withAuthorization(PlansPage),
  gqlQuery: PlansQueryDef,
  query: Query,
} satisfies RouteDefinition<PlansQuery>;
