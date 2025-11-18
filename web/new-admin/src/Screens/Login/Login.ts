import { graphql } from "react-relay";

export const LoginQueryDef = graphql`
  query LoginQuery {
    viewer {
      ... on Admin {
        id
      }
    }
  }
`;
