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
import NetworkError from "./NetworkError";

const wsClient = createClient({
  url: `${window.location.protocol === "https:" ? "wss" : "ws"}://${
    window.location.host
  }/graphql`,
});
const fetchQuery: FetchFunction = async (operation, variables) => {
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
    if (response.status === 401) {
      location.href = "/login";
      return {
        data: { errors: [{ message: "unauthorized" }] },
      };
    }
    const json = await response.json();
    if (json.errors) {
      console.error("GraphQL Error:", json.errors);
      // throw new GraphQLApiError(json.errors);
    }
    return json;
  } catch (err) {
    console.error("Fetch Error:", err);
    if (err instanceof GraphQLApiError) {
      throw err;
    }
    throw new NetworkError("We can't reach the internet!");
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
      sink as Sink<ExecutionResult<Record<string, unknown>, unknown>> // ts-ignore
    );
  });
};

const environment = new Environment({
  network: Network.create(fetchQuery, subscribe),
  store: new Store(new RecordSource()),
});

export default environment;
