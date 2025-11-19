import { graphql } from "react-relay";

export const PlansQueryDef = graphql`
  query PlansQuery {
    plans(first: 50) {
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
