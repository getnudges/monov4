import { graphql } from "react-relay";

export const PaidQueryDef = graphql`
  query PaidQuery {
    viewer {
      ... on Client {
        id
        ...Paid_ClientView
      }
    }
  }
`;
