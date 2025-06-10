import graphql from "babel-plugin-relay/macro";

import type { DashboardQuery } from "./__generated__/DashboardQuery.graphql";
import type { Dashboard_ClientView$key } from "./__generated__/Dashboard_ClientView.graphql";
import type { RelayRoute } from "@/Router/withRelay";
import { useMemo, useState } from "react";
import ErrorDialog from "@/components/ErrorDialog";
import { useFragment, useSubscription } from "react-relay";
import { Dashboard_OnClientUpdatedSubscription } from "./__generated__/Dashboard_OnClientUpdatedSubscription.graphql";
import { Redirect } from "wouter";
import { ClientDashboard } from "./client-dashboard";

function useClientUpdatedSubscription(
  id: string,
  onError?: (error: Error) => void
) {
  return useSubscription<Dashboard_OnClientUpdatedSubscription>(
    useMemo(
      () => ({
        subscription: graphql`
          subscription Dashboard_OnClientUpdatedSubscription($id: ID!) {
            onClientUpdated(id: $id) {
              ...Dashboard_ClientView
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

export const DashboardQueryDef = graphql`
  query DashboardQuery {
    viewer {
      ... on Client {
        id
        ...Dashboard_ClientView
      }
    }
  }
`;

type ClientViewProps = {
  viewer: Dashboard_ClientView$key;
};

const ClientView = ({ viewer }: Readonly<ClientViewProps>) => {
  const data = useFragment(
    graphql`
      fragment Dashboard_ClientView on Client {
        id
        name
        joinedDate
        locale
        phoneNumber
        slug
        subscriberCount
        subscription {
          id
          status
          startDate
          endDate
          priceTier {
            name
            price
          }
        }
        subscription {
          status
        }
      }
    `,
    viewer
  );
  return (
    <>
      <ClientDashboard
        client={{
          ...data,
          announcements: [
            {
              id: "ann_1",
              title: "New Feature Release",
              content: "We've just released our new analytics dashboard!",
              date: "2023-12-01T10:00:00Z",
            },
            {
              id: "ann_2",
              title: "Scheduled Maintenance",
              content:
                "System maintenance scheduled for Dec 15th from 2-4am EST",
              date: "2023-12-10T09:00:00Z",
            },
          ],
          recentSubscribers: [
            {
              id: "sub_1",
              name: "John Doe",
              email: "john@example.com",
              joinedDate: "2023-12-01T00:00:00Z",
            },
            {
              id: "sub_2",
              name: "Jane Smith",
              email: "jane@example.com",
              joinedDate: "2023-11-28T00:00:00Z",
            },
            {
              id: "sub_3",
              name: "Robert Johnson",
              email: "robert@example.com",
              joinedDate: "2023-11-25T00:00:00Z",
            },
            {
              id: "sub_4",
              name: "Emily Davis",
              email: "emily@example.com",
              joinedDate: "2023-11-20T00:00:00Z",
            },
            {
              id: "sub_5",
              name: "Michael Wilson",
              email: "michael@example.com",
              joinedDate: "2023-11-15T00:00:00Z",
            },
          ],
        }}
      />
    </>
  );
};

type ClientViewUpdaterProps = {
  id: string;
  viewer: Dashboard_ClientView$key;
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

export default function DashboardPage({
  data,
}: Readonly<RelayRoute<DashboardQuery>>) {
  if (!data.viewer?.id) {
    return <Redirect to="/portal" />;
  }

  return (
    <>
      <ClientViewUpdater viewer={data.viewer} id={data.viewer.id} />
    </>
  );
}
