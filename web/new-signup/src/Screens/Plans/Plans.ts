import { graphql } from "react-relay";

export const PlansQueryDef = graphql`
  query PlansQuery {
    plans(first: 3) {
      ...PlanTable_plans
    }
    viewer {
      ... on Client {
        id
        customerId
        ...Plans_ClientView
      }
    }
  }
`;
