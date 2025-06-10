import graphql from "babel-plugin-relay/macro";

import { RelayRoute } from "@/Router/withRelay";
import type { PlanQuery } from "./__generated__/PlanQuery.graphql";
import PlanEditor from "./PlanEditor";

export const PlanQueryDef = graphql`
  query PlanQuery($id: ID) {
    plan(id: $id) {
      ...PlanEditor_plan
    }
  }
`;

export default function PlanPage({ data }: Readonly<RelayRoute<PlanQuery>>) {
  return <PlanEditor plan={data.plan!} />;
}
