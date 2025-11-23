import { type RelayRoute } from "@/Router/withRelay";
import type { PlanQuery } from "./__generated__/PlanQuery.graphql";
import type { PlanEditor_plan$key } from "@/fragments/__generated__/PlanEditor_plan.graphql";
import { CreatePlan, EditPlan } from "./forms";

export default function PlanPage({
  data,
  params,
}: Readonly<RelayRoute<PlanQuery>>) {
  return <PlanFormsWrapper id={params?.id} plan={data.plan!} />;
}

function PlanFormsWrapper({
  id,
  plan,
}: {
  id?: string | null | undefined;
  plan: PlanEditor_plan$key;
}) {
  if (!id) {
    return <CreatePlan />;
  }
  return <EditPlan plan={plan} />;
}
  
