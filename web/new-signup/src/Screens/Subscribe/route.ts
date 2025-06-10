import Query from "./__generated__/SubscribeQuery.graphql";
import SubscribePage, { SubscribeQueryDef } from ".";

export default {
  path: "/subscribe/:slug",
  component: SubscribePage,
  gqlQuery: SubscribeQueryDef,
  query: Query,
};
