import { RelayEnvironmentProvider } from "react-relay";
import createRelayEnvironment from "./crateRelayEnvironment";
import { useLocation } from "wouter";
import { useCallback, useRef } from "react";
import type GraphQLApiError from "./GraphQLApiError";
import { useToaster } from "./components/Toaster";

const RelayEnvironmentProviderWrapper = ({
  children,
  onError,
}: {
  children: React.ReactNode;
  onError?: (error: GraphQLApiError) => void;
}) => {
  const { notify } = useToaster();
  const [, navTo] = useLocation();

  const onErrorWithToast = useCallback(
    (error: GraphQLApiError) => {
      if (error.status === 401) {
        return navTo("/login");
      }

      notify(
        error.errors.map((e) => e.message).join("\n\n"),
        "GraphQL Error",
        3000
      );
      onError?.(error);
    },
    [navTo, onError, notify]
  );

  const environment = useRef(createRelayEnvironment(onErrorWithToast)).current;

  return (
    <RelayEnvironmentProvider environment={environment}>
      {children}
    </RelayEnvironmentProvider>
  );
};

export default RelayEnvironmentProviderWrapper;
