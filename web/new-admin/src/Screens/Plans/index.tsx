import { Link, useLocation } from "wouter";

import { RelayRoute } from "@/Router/withRelay";
import type { PlansQuery } from "./__generated__/PlansQuery.graphql";
import { Button } from "@/components/ui/button";

export default function PlansPage({ data }: Readonly<RelayRoute<PlansQuery>>) {
  const [, setLocation] = useLocation();
  return (
    <div>
      <ul>
        {data?.plans?.edges?.map(({ node: plan }) => (
          <li key={plan.id}>
            <span>
              <Link to={`/plan/${encodeURIComponent(plan.id)}`}>
                {plan.name}
              </Link>
            </span>
          </li>
        ))}
      </ul>
      <Button onClick={() => setLocation("/plan")}>New Plan</Button>
    </div>
  );
}
