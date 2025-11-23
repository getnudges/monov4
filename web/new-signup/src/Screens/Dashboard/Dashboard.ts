import { graphql } from "react-relay";

export const DashboardQueryDef = graphql`
  query DashboardQuery {
    viewer {
      ... on Client {
        id
        ...Dashboard_ClientView
      }
    }
  }
`;
