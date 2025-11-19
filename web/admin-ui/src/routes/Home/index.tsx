import { Link, useLocation } from "wouter";

import { type RelayRoute } from "@/Router/withRelay";
import type { HomeQuery } from "./__generated__/HomeQuery.graphql";
import { useTranslation } from "react-i18next";

export default function HomePage({ data }: Readonly<RelayRoute<HomeQuery>>) {
  const [, setLocation] = useLocation();
  const { t } = useTranslation("home");

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
      <button onClick={() => setLocation("/plans")}>{t("viewPlans")}</button>
    </div>
  );
}
