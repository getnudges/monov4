import { RouteDefinition } from "@/Router/withRelay";
import Query, { type LoginQuery } from "./__generated__/LoginQuery.graphql";
import LoginPage, { LoginQueryDef } from ".";

export default {
  path: "/login",
  component: LoginPage,
  gqlQuery: LoginQueryDef,
  query: Query,
} satisfies RouteDefinition<LoginQuery>;
