import { graphql } from "relay-runtime";

const PlanEditor_plan = graphql`
  fragment PlanEditor_plan on Plan {
    id
    description
    features {
      planId
      aiSupport
      maxMessages
      supportTier
    }
    foreignServiceId
    iconUrl
    isActive
    name
    priceTiers {
      id
      planId
      createdAt
      description
      duration
      iconUrl
      name
      price
      foreignServiceId
      status
    }
  }
`;

export default PlanEditor_plan;
