import { graphql } from "react-relay";

export const HomeQueryDef = graphql`
  query HomeQuery {
    plans {
      edges {
        cursor
        node {
          id
          name
        }
      }
    }
  }
`;
