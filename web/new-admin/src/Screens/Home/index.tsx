import graphql from "babel-plugin-relay/macro";
import { Link, useLocation } from "wouter";

import { RelayRoute } from "@/Router/withRelay";
import type { HomeQuery } from "./__generated__/HomeQuery.graphql";
import { Button } from "@/components/ui/button";

export const HomeQueryDef = graphql`
  query HomeQuery {
    plans {
      edges {
        cursor
        node {
          id
          name
        }
      }
    }
  }
`;

export default function HomePage({ data }: Readonly<RelayRoute<HomeQuery>>) {
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
      <Button onClick={() => setLocation("/plans")}>View Plans</Button>
    </div>
  );
}
