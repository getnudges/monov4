import { RouteDefinition } from "@/Router/withRelay";
import Query, { type PortalQuery } from "./__generated__/PortalQuery.graphql";
import PortalPage, { PortalQueryDef } from ".";

export default {
  path: "/portal",
  component: PortalPage,
  gqlQuery: PortalQueryDef,
  query: Query,
} satisfies RouteDefinition<PortalQuery>;
