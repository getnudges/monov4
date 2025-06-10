import { memo } from "react";
import { Route, RouteProps, useParams } from "wouter";

import ErrorBoundary from "../ErrorBoundary";
import { RelayNavigatorProps } from "./withRelay";

type RelayNavigationScreenProps = RouteProps &
  Readonly<{
    Component: React.ComponentType<any>;
    includeQueryString: boolean;
  }>;

function queryStringToObject(queryString: string) {
  return [...new URLSearchParams(queryString).entries()].reduce(
    (acc, [key, value]) => ({ ...acc, [key]: value }),
    {}
  );
}

const RelayNavigationRoute = memo(function RelayNavigationScreen({
  Component,
  includeQueryString,
  ...props
}: RelayNavigationScreenProps) {
  const params = useParams();
  const queryVars = {
    ...Object.entries(params)
      .filter(([k]) => isNaN(+k))
      .reduce(
        (acc, [key, value]) => ({
          ...acc,
          [key]: decodeURIComponent(value ?? ""),
        }),
        {}
      ),
    ...(includeQueryString ? queryStringToObject(location.search ?? "") : {}),
  };

  return (
    <ErrorBoundary>
      <Component queryVars={queryVars} {...props} />
    </ErrorBoundary>
  );
});

RelayNavigationRoute.displayName = "RelayNavigationScreen";

export default function createRouterFactory(
  includeQueryString: boolean = false
) {
  return function RouterWrapper({ screens }: RelayNavigatorProps) {
    return screens.map(({ path, component, ...r }) => (
      <Route key={path} path={path}>
        <RelayNavigationRoute
          Component={component}
          {...r}
          includeQueryString={includeQueryString}
        />
      </Route>
    ));
  };
}
