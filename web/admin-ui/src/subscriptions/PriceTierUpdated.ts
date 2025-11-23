import { graphql } from "relay-runtime";

import { useSubscription } from "react-relay";
import { useMemo } from "react";

export function usePriceTierUpdatedSubscription(
  id: string,
  onError?: (error: Error) => void
) {
  return useSubscription(
    useMemo(
      () => ({
        subscription: graphql`
          subscription PriceTierUpdatedSubscription($id: ID!) {
            onPriceTierUpdated(id: $id) {
              ...PlanEditor_plan
            }
          }
        `,
        variables: { id },
        onError: onError,
      }),
      [id, onError]
    )
  );
}
