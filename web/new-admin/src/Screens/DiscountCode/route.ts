import { RouteDefinition } from "@/Router/withRelay";
import Query, {
  type DiscountCodeQuery,
} from "./__generated__/DiscountCodeQuery.graphql";
import DiscountCodePage, { DiscountCodeQueryDef } from ".";
import { withAuthorization } from "@/AuthProvider";

export default {
  path: "/discount-code/:id?",
  component: withAuthorization(DiscountCodePage),
  gqlQuery: DiscountCodeQueryDef,
  query: Query,
} satisfies RouteDefinition<DiscountCodeQuery>;
