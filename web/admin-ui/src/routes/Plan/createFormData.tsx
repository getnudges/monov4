import type { PlanEditor_plan$data } from "@/fragments/__generated__/PlanEditor_plan.graphql";
import type { PlanFormValues } from "./PlanForms";

export function createFormData(
  data: PlanEditor_plan$data | undefined | null
): PlanFormValues {
  return {
    ...(data ?? {}),
    id: data?.id,
    name: data?.name ?? "",
    description: data?.description ?? "",
    iconUrl: data?.iconUrl ?? "",
    isActive: data?.isActive ?? false,
    foreignServiceId: data?.foreignServiceId ?? "",
    features: {
      planId: data?.features?.planId || undefined,
      maxMessages: data?.features?.maxMessages ?? 0,
      supportTier: data?.features?.supportTier ?? "BASIC",
      aiSupport: data?.features?.aiSupport ?? false,
    },
    priceTiers:
      data?.priceTiers?.map((tier) => ({
        id: tier.id || undefined,
        planId: tier.planId ?? "",
        price: parseInt(tier.price ?? 0),
        duration: tier.duration ?? "P7D",
        name: tier.name ?? "",
        description: tier.description ?? "",
        iconUrl: tier.iconUrl ?? "",
        foreignServiceId: tier.foreignServiceId ?? "",
        status: tier.status ?? "ACTIVE",
      })) ?? [],
  };
}
