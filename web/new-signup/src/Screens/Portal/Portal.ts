import { graphql } from "react-relay";

export const PortalQueryDef = graphql`
  query PortalQuery {
    viewer {
      ... on Client {
        id
      }
    }
  }
`;
