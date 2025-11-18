import { graphql } from "react-relay";

export const DiscountCodeQueryDef = graphql`
  query DiscountCodeQuery($id: ID) {
    discountCode(id: $id) {
      ...DiscountCodeEditor_discountCode
    }
  }
`;
