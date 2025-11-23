import { graphql } from "relay-runtime";

import type { GraphQLSubscriptionConfig } from "relay-runtime";
import type { PlanUpdatedSubscription } from "./__generated__/PlanUpdatedSubscription.graphql";
import { useSubscription } from "react-relay";
import { useMemo } from "react";
import type { GraphQLSubscriptionError } from "@/types";

export function usePlanUpdatedSubscription(
  id: string,
  onError?: (error: GraphQLSubscriptionError[]) => void
) {
  return useSubscription<PlanUpdatedSubscription>(
    useMemo(
      () =>
        ({
          subscription: graphql`
            subscription PlanUpdatedSubscription($id: ID!) {
              onPlanUpdated(id: $id) {
                ...PlanEditor_plan
              }
            }
          `,
          variables: { id },
          onError(error) {
            if (onError) {
              onError(error as unknown as GraphQLSubscriptionError[]);
            }
          },
          onNext: (response) => {
            console.log("Plan updated via PlanUpdatedSubscription", response);
          },
        } satisfies GraphQLSubscriptionConfig<PlanUpdatedSubscription>),
      [id, onError]
    )
  );
}
