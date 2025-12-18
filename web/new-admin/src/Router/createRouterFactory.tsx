import { memo } from "react";
import { Route, RouteProps, useParams } from "wouter";

import ErrorBoundary from "../ErrorBoundary";
import { RelayNavigatorProps } from "./withRelay";

type RelayNavigationScreenProps = RouteProps &
  Readonly<{
    Component: React.ComponentType<any>;
    includeQueryString: boolean;
  }>;

/**
 * Converts URL query string to object.
 * @example "?foo=bar&baz=qux" → { foo: "bar", baz: "qux" }
 */
function queryStringToObject(queryString: string) {
  return [...new URLSearchParams(queryString).entries()].reduce(
    (acc, [key, value]) => ({ ...acc, [key]: value }),
    {}
  );
}

/**
 * Route component that extracts query variables from URL params and query string,
 * then passes them to the wrapped component for Relay query variables.
 *
 * Extracts:
 * - URL path params (e.g., /plan/:id → { id: "..." })
 * - Query string params (if includeQueryString=true) (e.g., ?foo=bar → { foo: "bar" })
 *
 * Filters out numeric array indices from wouter's params.
 */
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

/**
 * Creates a router factory that integrates wouter with Relay.
 *
 * @param includeQueryString - Whether to extract query string params as query variables
 * @returns Router component that renders route definitions as wouter Routes
 *
 * @example
 * ```typescript
 * const router = withRelay(
 *   createRouterFactory(true), // Include query string
 *   routes,
 *   LoadingScreen
 * );
 * ```
 */
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
