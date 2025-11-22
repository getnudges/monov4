import { RelayEnvironmentProvider } from "react-relay";
import createRelayEnvironment from "./crateRelayEnvironment";
import { useSnackbar } from "./components/Snackbar";
import type GraphQLApiError from "./GraphQLApiError";
import { useLocation } from "wouter";
import { useCallback, useRef } from "react";

const RelayEnvironmentProviderWrapper = ({
  children,
  onError,
}: {
  children: React.ReactNode;
  onError?: (error: GraphQLApiError) => void;
}) => {
  const { showSnackbar } = useSnackbar();
  const [, navTo] = useLocation();

  const onErrorWithSnackbar = useCallback(
    (error: GraphQLApiError) => {
      if (error.status === 401) {
        return navTo("/login");
      }

      showSnackbar(
        "error",
        error.errors.map((e) => e.message).join(", "),
        3000
      );
      onError?.(error);
    },
    [navTo, onError, showSnackbar]
  );

  const environment = useRef(
    createRelayEnvironment(onErrorWithSnackbar)
  ).current;

  return (
    <RelayEnvironmentProvider environment={environment}>
      {children}
    </RelayEnvironmentProvider>
  );
};

export default RelayEnvironmentProviderWrapper;
