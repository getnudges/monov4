import graphql from "babel-plugin-relay/macro";

import type { GraphQLSubscriptionConfig } from "relay-runtime";
import type { PlanUpdatedSubscription } from "./__generated__/PlanUpdatedSubscription.graphql";
import { useSubscription } from "react-relay";
import { useMemo } from "react";

export function usePlanUpdatedSubscription(
  id: string,
  onError?: (error: Error) => void
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
          onError: onError,
          onNext: (response) => {
            console.log("Plan updated via PlanUpdatedSubscription", response);
          },
        } satisfies GraphQLSubscriptionConfig<PlanUpdatedSubscription>),
      [id, onError]
    )
  );
}
