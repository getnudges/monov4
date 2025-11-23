import { graphql } from "relay-runtime";

const PlanFormPatchPriceTier = graphql`
  mutation PlanFormPatchPriceTierMutation(
    $patchPriceTierInput: PatchPriceTierInput!
  ) {
    patchPriceTier(input: $patchPriceTierInput) {
      plan {
        id
        ...PlanEditor_plan
      }
    }
  }
`;
export default PlanFormPatchPriceTier;
