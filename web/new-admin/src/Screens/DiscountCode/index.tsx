import { RelayRoute } from "@/Router/withRelay";
import type { DiscountCodeQuery } from "./__generated__/DiscountCodeQuery.graphql";
import DiscountEditor from "./DiscountCodeEditor";

export default function DiscountPage({
  data,
}: Readonly<RelayRoute<DiscountCodeQuery>>) {
  return <DiscountEditor discountCode={data.discountCode!} />;
}
