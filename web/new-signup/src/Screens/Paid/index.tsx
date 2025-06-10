import graphql from "babel-plugin-relay/macro";

import type { PaidQuery } from "./__generated__/PaidQuery.graphql";
import type { Paid_ClientView$key } from "./__generated__/Paid_ClientView.graphql";
import type { RelayRoute } from "@/Router/withRelay";
import { useMemo, useState } from "react";
import ErrorDialog from "@/components/ErrorDialog";
import { useFragment, useSubscription } from "react-relay";
import { Paid_OnClientUpdatedSubscription } from "./__generated__/Paid_OnClientUpdatedSubscription.graphql";
import { Redirect } from "wouter";

function useClientUpdatedSubscription(
  id: string,
  onError?: (error: Error) => void
) {
  return useSubscription<Paid_OnClientUpdatedSubscription>(
    useMemo(
      () => ({
        subscription: graphql`
          subscription Paid_OnClientUpdatedSubscription($id: ID!) {
            onClientUpdated(id: $id) {
              ...Paid_ClientView
            }
          }
        `,
        variables: { id },
        onError: onError,
        onNext: (response) => {
          console.log("Client updated", response);
        },
      }),
      [id, onError]
    )
  );
}

export const PaidQueryDef = graphql`
  query PaidQuery {
    viewer {
      ... on Client {
        id
        ...Paid_ClientView
      }
    }
  }
`;

type ClientViewProps = {
  viewer: Paid_ClientView$key;
};

const ClientView = ({ viewer }: Readonly<ClientViewProps>) => {
  const data = useFragment(
    graphql`
      fragment Paid_ClientView on Client {
        subscription {
          status
        }
      }
    `,
    viewer
  );
  return (
    <>
      <p>
        You Subscription status is{" "}
        <strong>{data!.subscription?.status ?? "Pending..."}</strong>
      </p>
    </>
  );
};

type ClientViewUpdaterProps = {
  id: string;
  viewer: Paid_ClientView$key;
};

const ClientViewUpdater = ({
  viewer,
  id,
}: Readonly<ClientViewUpdaterProps>) => {
  const [subError, setSubError] = useState<Error | null>(null);
  useClientUpdatedSubscription(id, (e) => setSubError(e));
  return (
    <>
      <ClientView viewer={viewer} />
      {!!subError && (
        <ErrorDialog
          error={subError}
          startOpen={!!subError}
          title="Update Error"
          onClose={() => setSubError(null)}
        />
      )}
    </>
  );
};

export default function PaidPage({ data }: Readonly<RelayRoute<PaidQuery>>) {
  if (!data.viewer?.id) {
    return <Redirect to="/" />;
  }

  return (
    <div>
      <h1>Success!</h1>
      <ClientViewUpdater viewer={data.viewer} id={data.viewer.id} />
    </div>
  );
}
