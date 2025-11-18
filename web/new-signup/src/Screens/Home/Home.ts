import { graphql } from "react-relay";

export const HomeQueryDef = graphql`
  query HomeQuery {
    totalClients
    totalSubscribers
  }
`;
