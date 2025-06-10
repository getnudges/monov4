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
        admin: id
      }
      ... on Client {
        client: id
      }
      ... on Subscriber {
        subscriber: id
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

export const useAuthorized = () => {
  const [authorized] = useContext(AuthContext);
  return authorized;
};
export const useAuthorization = () => useContext(AuthContext);

export function withAuthorization<T extends RelayRoute<OperationType>>(
  WrappedComponent: React.ComponentType<T>,
  allowedRoles: string[] = []
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
          const roles = [...Object.entries(check?.viewer ?? {})]
            .filter(([, value]) => !!value)
            .map(([key]) => key);
          const isAuthorized = roles.some((role) =>
            allowedRoles.includes(role)
          );
          if (isAuthorized) {
            authorize();
            return;
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
