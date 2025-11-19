import { type RelayRoute } from "@/Router/withRelay";
import type { PlanQuery } from "./__generated__/PlanQuery.graphql";
import PlanEditor from "./PlanEditor";

export default function PlanPage({ data }: Readonly<RelayRoute<PlanQuery>>) {
  return <PlanEditor plan={data.plan!} />;
}
