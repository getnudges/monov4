import Query from "./__generated__/SignUpQuery.graphql";
import SignUpPage, { SignUpQueryDef } from ".";

export default {
  path: "/signup",
  component: SignUpPage,
  gqlQuery: SignUpQueryDef,
  query: Query,
};
