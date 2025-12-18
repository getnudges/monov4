import {
  Environment,
  Network,
  RecordSource,
  Store,
  Observable,
  type SubscribeFunction,
  type FetchFunction,
} from "relay-runtime";
import { createClient, type ExecutionResult, type Sink } from "graphql-ws";

import GraphQLApiError from "./GraphQLApiError";

const wsClient = createClient({
  url: `${window.location.protocol === "https:" ? "wss" : "ws"}://${
    window.location.host
  }/graphql`,
});
const createFetchQuery =
  (
    onError: (error: GraphQLApiError) => void,
    onFail?: (error: Error) => void
  ): FetchFunction =>
  async (operation, variables) => {
    try {
      const response = await fetch(`/graphql`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
        },
        body: JSON.stringify({
          query: operation.text,
          variables,
        }),
      });

      const json = await response.json();
      if (json.errors) {
        if (onError) {
          onError(new GraphQLApiError(response.status, json.errors));
        } else {
          return Promise.reject(
            new GraphQLApiError(response.status, json.errors)
          );
        }
      }
      return json;
    } catch (err) {
      if (onFail) {
        onFail(err as Error);
      } else {
        return Promise.reject(err as Error);
      }
    }
  };

const subscribe: SubscribeFunction = (operation, variables) => {
  return Observable.create((sink: unknown) => {
    return wsClient.subscribe(
      {
        operationName: operation.name,
        query: operation.text!,
        variables,
      },
      sink as Sink<ExecutionResult<Record<string, unknown>, unknown>> // ts-ignore,
    );
  });
};

/**
 * Creates a Relay Environment configured for the new-admin app.
 *
 * Configuration:
 * - Network layer with fetch (HTTP POST to /graphql)
 * - WebSocket subscription layer (ws:// or wss://)
 * - In-memory normalized cache (Store + RecordSource)
 * - Custom error handling via onError callback
 *
 * @param onError - Callback for GraphQL errors (network/parse errors)
 * @returns Configured Relay Environment
 *
 * @example
 * ```typescript
 * const environment = crateRelayEnvironment((error) => {
 *   console.error('GraphQL error:', error);
 *   showErrorDialog(error);
 * });
 * ```
 */
const crateRelayEnvironment = (onError: (error: GraphQLApiError) => void) => {
  return new Environment({
    network: Network.create(createFetchQuery(onError), subscribe),
    store: new Store(new RecordSource()),
  });
};

export default crateRelayEnvironment;
