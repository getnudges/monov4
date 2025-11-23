import Query from "./__generated__/SignUpQuery.graphql";
import { SignUpQueryDef } from "./SignUp";
import React from "react";

export default {
  path: "/signup",
  component: React.lazy(() => import(".")),
  gqlQuery: SignUpQueryDef,
  query: Query,
};
