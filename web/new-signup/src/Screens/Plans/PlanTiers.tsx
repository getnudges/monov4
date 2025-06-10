import graphql from "babel-plugin-relay/macro";

import { useFragment } from "react-relay";
import type { PlanTiers_plan$key } from "./__generated__/PlanTiers_plan.graphql";

import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { useMemo } from "react";

type PricingTierProps = Readonly<{
  duration: string;
  plan: PlanTiers_plan$key;
  onSelectPrice: (id: string) => () => void;
}>;

const PlanTiers: React.FC<PricingTierProps> = ({
  plan,
  duration,
  onSelectPrice: onSelectPlan,
}) => {
  const data = useFragment(
    graphql`
      fragment PlanTiers_plan on Plan {
        id
        name
        description
        iconUrl
        features {
          maxMessages
          aiSupport
          supportTier
        }
        priceTiers {
          id
          price
          duration
          iconUrl
          name
          foreignServiceId
        }
      }
    `,
    plan
  );

  const priceTiers = useMemo(
    () =>
      data.priceTiers
        .filter((tier) => tier.duration === duration)
        .map((tier) => {
          return {
            ...tier,
            price: new Intl.NumberFormat("en-US", {
              style: "currency",
              currency: "USD",
            }).format(tier.price),
          };
        }),
    [data.priceTiers, duration]
  );

  const [{ price, foreignServiceId }] = priceTiers;

  return (
    <Card className="w-full max-w-sm">
      <CardHeader>
        <CardTitle>{data.name}</CardTitle>
        <CardDescription>{data.description}</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="text-4xl font-bold mb-4">
          {price}
          <span className="text-sm font-normal">
            /{duration === "P30D" ? "month" : "week"}
          </span>
        </div>
        <ul className="space-y-2">
          <li className="flex items-center">
            <CheckIcon />
            Max Messages: {data.features?.maxMessages}
          </li>
          <li className="flex items-center">
            <CheckIcon />
            Support Tier: {data.features?.supportTier}
          </li>
          {data.features?.aiSupport && (
            <li className="flex items-center">
              <CheckIcon />
              AI Generated Messages
            </li>
          )}
        </ul>
      </CardContent>
      <CardFooter>
        <Button onClick={onSelectPlan(foreignServiceId!)} className="w-full">
          Choose Plan
        </Button>
      </CardFooter>
    </Card>
  );
};

export default PlanTiers;
function CheckIcon() {
  return (
    <svg
      className="w-4 h-4 mr-2 text-green-500"
      fill="none"
      strokeLinecap="round"
      strokeLinejoin="round"
      strokeWidth="2"
      viewBox="0 0 24 24"
      stroke="currentColor"
    >
      <path d="M5 13l4 4L19 7"></path>
    </svg>
  );
}
