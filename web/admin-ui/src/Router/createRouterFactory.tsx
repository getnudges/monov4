/* eslint-disable react-refresh/only-export-components */
import { memo, Suspense } from "react";
import { Route, type RouteProps, useParams } from "wouter";

import ErrorBoundary from "../ErrorBoundary";
import type { RelayNavigatorProps } from "./withRelay";
import LoadingScreen from "@/LoadingScreen";

type RelayNavigationScreenProps = RouteProps &
  Readonly<{
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
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
        <Suspense fallback={<LoadingScreen />}>
          <RelayNavigationRoute
            Component={component}
            {...r}
            includeQueryString={includeQueryString}
          />
        </Suspense>
      </Route>
    ));
  };
}
