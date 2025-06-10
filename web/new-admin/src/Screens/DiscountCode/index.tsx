import graphql from "babel-plugin-relay/macro";

import { RelayRoute } from "@/Router/withRelay";
import type { DiscountCodeQuery } from "./__generated__/DiscountCodeQuery.graphql";
import DiscountEditor from "./DiscountCodeEditor";

export const DiscountCodeQueryDef = graphql`
  query DiscountCodeQuery($id: ID) {
    discountCode(id: $id) {
      ...DiscountCodeEditor_discountCode
    }
  }
`;

export default function DiscountPage({
  data,
}: Readonly<RelayRoute<DiscountCodeQuery>>) {
  return <DiscountEditor discountCode={data.discountCode!} />;
}
