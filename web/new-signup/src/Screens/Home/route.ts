import { RouteDefinition } from "@/Router/withRelay";
import Query, { type HomeQuery } from "./__generated__/HomeQuery.graphql";
import HomePage, { HomeQueryDef } from ".";

export default {
  path: "/",
  component: HomePage,
  gqlQuery: HomeQueryDef,
  query: Query,
} satisfies RouteDefinition<HomeQuery>;
