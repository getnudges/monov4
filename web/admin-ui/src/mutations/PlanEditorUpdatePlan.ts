import { graphql } from "relay-runtime";

const PlanEditorUpdatePlan = graphql`
  mutation PlanEditorUpdatePlanMutation($updatePlanInput: UpdatePlanInput!) {
    updatePlan(input: $updatePlanInput) {
      plan {
        id
        ...PlanEditor_plan
      }
    }
  }
`;

export default PlanEditorUpdatePlan;
