import type { GraphQLError } from "graphql";

export default class GraphQLApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly errors: GraphQLError[]
  ) {
    const message = errors.map((e, i) => `${i + 1}) ${e.message}`).join(" ");
    super(message);
    this.name = this.constructor.name;
  }
}
