import React from "react";
import { Environment, Network, RecordSource, Store } from "relay-runtime";
import { RelayEnvironmentProvider } from "react-relay";

/**
 * Creates a mock Relay environment for Storybook stories.
 *
 * Provides a simple fetch function that returns mock data.
 * Use this to demonstrate Relay patterns without a real GraphQL backend.
 *
 * @param mockData - Mock GraphQL response data
 * @returns Mock Relay Environment
 */
export function createMockRelayEnvironment(mockData: any = {}) {
  const mockFetch = () => {
    return Promise.resolve(mockData);
  };

  return new Environment({
    network: Network.create(mockFetch as any),
    store: new Store(new RecordSource()),
  });
}

/**
 * Decorator for Storybook stories that need a Relay environment.
 *
 * @example
 * ```tsx
 * export default {
 *   title: 'Patterns/Relay Screen',
 *   decorators: [withMockRelay({ data: { plan: { id: '1', name: 'Basic' } } })],
 * };
 * ```
 */
export function withMockRelay(mockData: any = {}) {
  return (Story: React.ComponentType) => {
    const environment = createMockRelayEnvironment(mockData);

    return (
      <RelayEnvironmentProvider environment={environment}>
        <Story />
      </RelayEnvironmentProvider>
    );
  };
}

/**
 * Mock implementation of useRelayScreenContext for stories.
 * Use this to provide a mock refresh function and variables.
 */
export function mockRelayScreenContext<T = any>(variables: T = {} as T) {
  return {
    queryReference: null,
    refresh: (newVars?: T) => {
      console.log("Mock refresh called with:", newVars || variables);
    },
    variables,
  };
}
