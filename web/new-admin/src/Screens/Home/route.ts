import { RouteDefinition } from "@/Router/withRelay";
import Query, { type HomeQuery } from "./__generated__/HomeQuery.graphql";
import HomePage, { HomeQueryDef } from ".";
import { withAuthorization } from "@/AuthProvider";

export default {
  path: "/",
  component: withAuthorization(HomePage),
  gqlQuery: HomeQueryDef,
  query: Query,
} satisfies RouteDefinition<HomeQuery>;
