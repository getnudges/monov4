import graphql from 'babel-plugin-relay/macro';

import { useFragment } from 'react-relay';
import type { PlanTable_plans$key } from './__generated__/PlanTable_plans.graphql';
import { Switch } from "@/components/ui/switch";
import PlanTiers from "./PlanTiers";
import { useState } from "react";

type Props = Readonly<{
  plans: PlanTable_plans$key;
  onSelectPrice: (id: string) => () => void;
}>;

const PlanTable = ({ plans, onSelectPrice }: Props) => {
  const data = useFragment(
    graphql`
      fragment PlanTable_plans on PlansConnection {
        edges {
          node {
            id
            ...PlanTiers_plan
          }
        }
      }
    `,
    plans
  );
  const [duration, setDuration] = useState("P7D");

  return (
    <div className="container mx-auto py-12">
      <div className="text-center mb-8">
        <h2 className="text-3xl font-bold mb-4">Choose Your Plan</h2>
        <div className="flex items-center justify-center space-x-2">
          <span className={`text-sm ${duration == "P7D" ? "font-bold" : ""}`}>
            Weekly
          </span>
          <Switch
            checked={duration === "P30D"}
            onCheckedChange={(monthly) => setDuration(monthly ? "P30D" : "P7D")}
          />
          <span className={`text-sm ${duration == "P30D" ? "font-bold" : ""}`}>
            Monthly
          </span>
        </div>
      </div>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
        {data?.edges?.map(({ node: plan }) => (
          <PlanTiers
            key={plan.id}
            plan={plan}
            onSelectPrice={onSelectPrice}
            duration={duration}
          />
        ))}
      </div>
    </div>
  );
};

export default PlanTable;
