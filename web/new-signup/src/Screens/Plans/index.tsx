import graphql from 'babel-plugin-relay/macro';

import type { PlansQuery } from "./__generated__/PlansQuery.graphql";
import type { RelayRoute } from "../../Router/withRelay";
import PlanTable from "./PlanTable";
import { useMemo, useState } from "react";
import { useFragment, useMutation, useSubscription } from "react-relay";
import type { Plans_CreateCheckoutSessionMutation } from "./__generated__/Plans_CreateCheckoutSessionMutation.graphql";
import ErrorDialog from "@/components/ErrorDialog";
import type { Plans_OnClientUpdatedSubscription } from "./__generated__/Plans_OnClientUpdatedSubscription.graphql";
import type { Plans_ClientView$key } from "./__generated__/Plans_ClientView.graphql";

function useClientUpdatedSubscription(
  id: string,
  onError?: (error: Error) => void
) {
  return useSubscription<Plans_OnClientUpdatedSubscription>(
    useMemo(
      () => ({
        subscription: graphql`
          subscription Plans_OnClientUpdatedSubscription($id: ID!) {
            onClientUpdated(id: $id) {
              ...Plans_ClientView
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

export const PlansQueryDef = graphql`
  query PlansQuery {
    plans(first: 3) {
      ...PlanTable_plans
    }
    viewer {
      ... on Client {
        id
        customerId
        ...Plans_ClientView
      }
    }
  }
`;

type ClientViewProps = {
  viewer: Plans_ClientView$key;
};

const CustomerIdDep = ({
  viewer,
  children,
}: React.PropsWithChildren<ClientViewProps>) => {
  const data = useFragment(
    graphql`
      fragment Plans_ClientView on Client {
        customerId
      }
    `,
    viewer
  );

  if (!data.customerId) {
    return null;
  }
  return <>{children}</>;
};

type ClientViewUpdaterProps = {
  id: string;
  viewer: Plans_ClientView$key;
};

const ClientChecker = ({
  viewer,
  id,
  children,
}: React.PropsWithChildren<ClientViewUpdaterProps>) => {
  const [subError, setSubError] = useState<Error | null>(null);
  useClientUpdatedSubscription(id, (e) => setSubError(e));
  return (
    <>
      <CustomerIdDep viewer={viewer}>{children}</CustomerIdDep>
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

export default function PlansPage({ data }: Readonly<RelayRoute<PlansQuery>>) {
  const [createCheckoutSessionError, setCreateCheckoutSessionError] =
    useState<Error | null>(null);
  const [createCheckoutSessionMutation] =
    useMutation<Plans_CreateCheckoutSessionMutation>(
      graphql`
        mutation Plans_CreateCheckoutSessionMutation(
          $input: CreateCheckoutSessionInput!
        ) {
          createCheckoutSession(input: $input) {
            checkoutSession {
              checkoutUrl
            }
            errors {
              ... on Error {
                message
              }
            }
          }
        }
      `
    );

  const onSelectPrice = (id: string) => () => {
    createCheckoutSessionMutation({
      variables: {
        input: {
          customerId: data.viewer!.customerId!,
          priceForeignServiceId: id,
          cancelUrl: `${window.location.origin}/cancel`,
          successUrl: `${window.location.origin}/paid`,
        },
      },
      onError(error) {
        setCreateCheckoutSessionError(error);
      },
      onCompleted({ createCheckoutSession }) {
        if (!createCheckoutSession?.checkoutSession?.checkoutUrl) {
          return setCreateCheckoutSessionError(new Error("no checkout url"));
        }
        if (
          createCheckoutSession?.errors &&
          createCheckoutSession.errors.length > 0
        ) {
          return setCreateCheckoutSessionError(
            new Error(createCheckoutSession.errors[0].message) // TODO: show all errors
          );
        }

        console.log(
          "redirecting to",
          createCheckoutSession.checkoutSession.checkoutUrl
        );
        window.location.href =
          createCheckoutSession.checkoutSession.checkoutUrl;
      },
    });
  };

  if (!data.viewer?.id) {
    return <div>loading...</div>;
  }

  return (
    <ClientChecker id={data.viewer.id} viewer={data.viewer}>
      <PlanTable plans={data.plans!} onSelectPrice={onSelectPrice} />
      {createCheckoutSessionError && (
        <ErrorDialog
          title="Failed to create checkout session"
          error={createCheckoutSessionError} // TODO: show all errors
          startOpen={!!createCheckoutSessionError}
          onClose={() => setCreateCheckoutSessionError(null)}
        />
      )}
    </ClientChecker>
  );
}
