import { graphql } from "react-relay";

export const PlanQueryDef = graphql`
  query PlanQuery($id: ID) {
    plan(id: $id) {
      ...PlanEditor_plan
    }
  }
`;
