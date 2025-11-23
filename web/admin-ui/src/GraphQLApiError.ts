import type { GraphQLError } from "graphql";

export default class GraphQLApiError extends Error {
  public readonly status: number;
  public readonly errors: GraphQLError[];

  constructor(status: number, errors: GraphQLError[]) {
    const message = errors.map((e, i) => `${i + 1}) ${e.message}`).join(" ");
    super(message);
    this.name = this.constructor.name;
    this.status = status;
    this.errors = errors;
  }
}
