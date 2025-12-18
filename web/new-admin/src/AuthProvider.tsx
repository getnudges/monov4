import graphql from "babel-plugin-relay/macro";

import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
} from "react";

import { RelayRoute } from "./Router/withRelay";
import { OperationType } from "relay-runtime";
import { useLocation } from "wouter";
import { useRelayEnvironment, fetchQuery } from "react-relay";
import type { AuthProviderRoleQuery } from "./__generated__/AuthProviderRoleQuery.graphql";

export const AuthRoleQueryDef = graphql`
  query AuthProviderRoleQuery {
    viewer {
      ... on Admin {
        id
      }
    }
  }
`;

type AuthContextType = [boolean, () => void, () => void];

const AuthContext = createContext<AuthContextType>({} as AuthContextType);

export const AuthProvider = ({ children }: React.PropsWithChildren) => {
  const [authorized, setAuthorized] = useState(false);

  const authorize = () => {
    setAuthorized(true);
  };
  const logOut = useCallback(() => {
    if (authorized) {
      setAuthorized(false);
    }
  }, [authorized]);

  return (
    <AuthContext.Provider value={[authorized, authorize, logOut]}>
      {children}
    </AuthContext.Provider>
  );
};

/**
 * Hook to check if user is authorized (boolean).
 */
export const useAuthorized = () => {
  const [authorized] = useContext(AuthContext);
  return authorized;
};

/**
 * Hook to access full auth context (authorized state, authorize, logOut).
 */
export const useAuthorization = () => useContext(AuthContext);

/**
 * Higher-order component that protects routes by requiring authentication.
 *
 * Flow:
 * 1. Checks if user is authorized via context
 * 2. If not authorized and not on /login, queries GraphQL for viewer role
 * 3. If viewer exists, authorizes user
 * 4. If no viewer, redirects to /login
 * 5. While checking, returns null (loading state)
 *
 * @param WrappedComponent - Component to protect
 * @returns Protected component that only renders when authorized
 *
 * @example
 * ```typescript
 * export default withAuthorization(MyProtectedScreen);
 * ```
 */
export function withAuthorization<T extends RelayRoute<OperationType>>(
  WrappedComponent: React.ComponentType<T>
) {
  const displayName =
    WrappedComponent.displayName || WrappedComponent.name || "Component";

  const ComponentWithTheme = (
    props: Omit<T, keyof RelayRoute<OperationType>>
  ) => {
    const [location, navTo] = useLocation();
    const [authorized, authorize, logOut] = useContext(AuthContext);
    const env = useRelayEnvironment();
    useEffect(() => {
      if (authorized || location === "/login") {
        return;
      }
      fetchQuery<AuthProviderRoleQuery>(env, AuthRoleQueryDef, {})
        .toPromise()
        .then((check) => {
          if (check?.viewer?.id) {
            return authorize();
          }

          logOut();
          return navTo("/login");
        });
    }, [env, navTo, location, authorized, authorize, logOut]);

    if (!authorized) {
      return null;
    }

    return <WrappedComponent {...(props as T)} />;
  };

  ComponentWithTheme.displayName = `withAuthorization(${displayName})`;

  return ComponentWithTheme;
}
