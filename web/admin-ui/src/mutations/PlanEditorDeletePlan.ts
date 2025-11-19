import { graphql } from "relay-runtime";

const PlanEditorDeletePlan = graphql`
  mutation PlanEditorDeletePlanMutation($deletePlanInput: DeletePlanInput!) {
    deletePlan(input: $deletePlanInput) {
      plan {
        id
        ...PlanEditor_plan
      }
    }
  }
`;

export default PlanEditorDeletePlan;
