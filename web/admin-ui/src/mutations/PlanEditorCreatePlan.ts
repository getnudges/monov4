import { graphql } from "relay-runtime";

const PlanEditorCreatePlan = graphql`
  mutation PlanEditorCreatePlanMutation($createPlanInput: CreatePlanInput!) {
    createPlan(input: $createPlanInput) {
      plan {
        id
        ...PlanEditor_plan
      }
    }
  }
`;

export default PlanEditorCreatePlan;
