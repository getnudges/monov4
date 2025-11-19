import { graphql } from "relay-runtime";

const PlanFormDeletePriceTier = graphql`
  mutation PlanFormDeletePriceTierMutation(
    $deletePriceTierInput: DeletePriceTierInput!
  ) {
    deletePriceTier(input: $deletePriceTierInput) {
      plan {
        id
        ...PlanEditor_plan
      }
    }
  }
`;

export default PlanFormDeletePriceTier;
