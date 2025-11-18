/* eslint-disable @typescript-eslint/no-explicit-any */
/* eslint-disable react-refresh/only-export-components */
import { GraphQLError } from "graphql";
import React, {
  useCallback,
  useContext,
  useMemo,
  type ReactElement,
} from "react";
import {
  type PreloadedQuery,
  usePreloadedQuery,
  useQueryLoader,
} from "react-relay";
import type {
  ConcreteRequest,
  GraphQLTaggedNode,
  OperationType,
} from "relay-runtime";

export type RelayNavigatorContextType = Readonly<{
  suspenseFallback: React.ReactNode | ReactElement | (() => ReactElement);
}>;
export type RelayScreenContextType<T extends OperationType> = Readonly<{
  readonly queryReference: T;
  readonly refresh: (params?: T["variables"]) => void;
  readonly variables: T["variables"];
}>;

const RelayNavigatorContext = React.createContext<RelayNavigatorContextType>(
  {} as RelayNavigatorContextType
);
const RelayScreenContext = React.createContext<RelayScreenContextType<any>>(
  {} as RelayScreenContextType<any>
);

export function useRelayNavigatorContext() {
  return useContext(RelayNavigatorContext);
}
export function useRelayScreenContext<T extends OperationType = never>() {
  return useContext(
    RelayScreenContext as React.Context<RelayScreenContextType<T>>
  );
}

type ComponentWrapperProps = Readonly<{
  Component: React.ComponentType<any>;
  [key: string]: any;
}>;

type RelayComponentWrapperProps<TQuery extends OperationType> = Readonly<{
  Component: React.ComponentType<any>;
  gqlQuery: GraphQLTaggedNode;
  queryReference: PreloadedQuery<TQuery>;
  params?: TQuery["variables"];
}>;

const ComponentWrapper = React.forwardRef<unknown, ComponentWrapperProps>(
  ({ Component, ...props }, ref) => {
    return <Component ref={ref} {...props} />;
  }
);
ComponentWrapper.displayName = "RelayComponentWrapper";

function RelayComponentWrapper<T extends OperationType>({
  Component,
  gqlQuery,
  queryReference,
  ...props
}: RelayComponentWrapperProps<T>) {
  const data = usePreloadedQuery(gqlQuery, queryReference);
  // TODO: how do I pass the `errors` field in the response to the component?
  return <Component Component={Component} data={data} {...props} />;
}

export type RelayRoute<T extends OperationType> = Readonly<{
  data: T["response"];
  params?: T["variables"];
  errors?: ReadonlyArray<GraphQLError>;
}>;

// eslint-disable-next-line @typescript-eslint/no-unused-vars
type RelayRouteDefinition = {
  query: ConcreteRequest; // PreloadableConcreteRequest<T>;
  gqlQuery: GraphQLTaggedNode;
  fetchPolicy?: "store-or-network" | "store-and-network" | "network-only";
  skeleton?: React.ReactNode | Element | (() => Element);
};

type BaseRouteDefinition = {
  path: string;
  component: React.ComponentType<any>;
};

export type RouteDefinition<T extends OperationType = never> = T extends never
  ? Readonly<BaseRouteDefinition>
  : Readonly<BaseRouteDefinition & RelayRouteDefinition>;

type RelayScreenWrapperProps<T extends OperationType> = RouteDefinition<T> & {
  readonly queryVars: {
    readonly [key: string]: any;
  };
};

function RelayScreenWrapper<T extends OperationType>({
  fetchPolicy,
  query,
  skeleton,
  component,
  queryVars,
  gqlQuery,
  ...props
}: RelayScreenWrapperProps<T>) {
  const { suspenseFallback } = useRelayNavigatorContext();
  const [queryReference, loadQuery, disposeQuery] = useQueryLoader(query);

  const vars = useMemo(() => ({ ...queryVars }), [queryVars]);

  React.useEffect(() => {
    if (!queryReference) {
      loadQuery(vars, { fetchPolicy });
    }
  }, [loadQuery, disposeQuery, queryReference, vars, fetchPolicy]);

  React.useEffect(() => () => disposeQuery(), [disposeQuery]);

  const refresh = useCallback(
    (newVars: typeof queryVars) => {
      loadQuery(newVars, { fetchPolicy: "network-only" });
    },
    [loadQuery]
  );
  const screenContextState = useMemo(
    () => ({ queryReference, refresh, variables: vars }),
    [queryReference, vars, refresh]
  );

  if (!queryReference) {
    return <ComponentWrapper Component={skeleton ?? suspenseFallback} />;
  }

  return (
    <RelayScreenContext.Provider value={screenContextState}>
      <React.Suspense
        fallback={<ComponentWrapper Component={skeleton ?? suspenseFallback} />}
      >
        <RelayComponentWrapper
          Component={component}
          queryReference={queryReference}
          gqlQuery={gqlQuery}
          params={vars}
          {...props}
        />
      </React.Suspense>
    </RelayScreenContext.Provider>
  );
}

export type RelayWrapperProps = Readonly<{
  [key: string]: any;
}>;

export type RelayNavigatorProps<T extends OperationType = OperationType> =
  Readonly<{
    screens: RouteDefinition<T>[];
  }>;

export default function withRelay<T extends OperationType = OperationType>(
  WrappedNavigator: React.ComponentType<any>,
  routeDefList: RouteDefinition<T>[],
  suspenseFallback: React.ReactNode | ReactElement | (() => ReactElement)
) {
  const screens = routeDefList.map(({ query, component, ...rest }) => {
    return {
      ...rest,
      component: function RelayQueryScreen(props: RelayScreenWrapperProps<T>) {
        return (
          <RelayScreenWrapper {...props} query={query} component={component} />
        );
      },
    };
  });

  return function RelayContextWrapper(wrapperProps: any) {
    const contextValue = React.useMemo(() => ({ suspenseFallback }), []);
    return (
      <RelayNavigatorContext.Provider value={contextValue}>
        <WrappedNavigator {...wrapperProps} screens={screens} />
      </RelayNavigatorContext.Provider>
    );
  };
}
