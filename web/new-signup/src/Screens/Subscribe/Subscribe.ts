import { graphql } from "react-relay";

export const SubscribeQueryDef = graphql`
  query SubscribeQuery($slug: String!) {
    clientBySlug(slug: $slug) {
      id
      name
    }
  }
`;
