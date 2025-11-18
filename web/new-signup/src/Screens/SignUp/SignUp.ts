import { graphql } from "react-relay";

export const SignUpQueryDef = graphql`
  query SignUpQuery {
    viewer {
      ... on Client {
        id
        subscriptionId
        subscription {
          status
        }
      }
    }
    totalClients
  }
`;
