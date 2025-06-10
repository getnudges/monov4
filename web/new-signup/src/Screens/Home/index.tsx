import graphql from "babel-plugin-relay/macro";

import { type HomeQuery } from "./__generated__/HomeQuery.graphql";
import type { RelayRoute } from "@/Router/withRelay";
import { Link } from "wouter";

export const HomeQueryDef = graphql`
  query HomeQuery {
    totalClients
    totalSubscribers
  }
`;

export default function HomePage({ data }: Readonly<RelayRoute<HomeQuery>>) {
  return (
    <div>
      <h1>Home</h1>
      <p>Number of Clients: {data.totalClients}</p>
      <p>Number of Subscribers: {data.totalSubscribers}</p>
      <Link to="/signup">Sign up</Link>
    </div>
  );
}
