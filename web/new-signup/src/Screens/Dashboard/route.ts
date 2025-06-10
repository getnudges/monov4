import { RouteDefinition } from "@/Router/withRelay";
import Query, {
  type DashboardQuery,
} from "./__generated__/DashboardQuery.graphql";
import DashboardPage, { DashboardQueryDef } from ".";
import { withAuthorization } from "@/AuthProvider";

export default {
  path: "/dashboard",
  component: withAuthorization(DashboardPage, ["client"]),
  gqlQuery: DashboardQueryDef,
  query: Query,
} satisfies RouteDefinition<DashboardQuery>;
