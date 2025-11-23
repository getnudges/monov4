import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import * as z from "zod";
import type { PlanEditor_plan$data } from "@/fragments/__generated__/PlanEditor_plan.graphql";

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

const planSchema = z.object({
  id: z.string().optional(),
  name: z.string().min(1, "Name is required").max(100),
  description: z.string(),
  iconUrl: z.url("Invalid URL").optional().or(z.literal("")),
  isActive: z.boolean(),
  foreignServiceId: z.string().max(200).optional().readonly(),
  features: z.object({
    planId: z.string().optional(),
    maxMessages: z.number().int().min(1),
    supportTier: z.union([
      z.literal("BASIC"),
      z.literal("STANDARD"),
      z.literal("PREMIUM"),
      z.literal("%future added value"),
    ]),
    aiSupport: z.boolean(),
  }),
  priceTiers: z.array(
    z.object({
      id: z.string().optional(),
      planId: z.string().optional(),
      price: z.number().min(1),
      duration: z.union([
        z.literal("P7D"),
        z.literal("P30D"),
        z.literal("P365D"),
        z.literal("%future added value"),
      ]),
      status: z
        .union([
          z.literal("ACTIVE"),
          z.literal("INACTIVE"),
          z.literal("ARCHIVED"),
          z.literal("DELETED"),
          z.literal("%future added value"),
        ])
        .optional(),
      name: z.string().min(1, "Name is required").max(100),
      description: z.string().optional(),
      foreignServiceId: z.string().optional(),
      iconUrl: z
        .string()
        .max(1000)
        .url("Invalid URL")
        .optional()
        .or(z.literal("")),
    })
  ),
  // discountCodes: z.array(
  //   z.object({
  //     id: z.string().optional(),
  //     code: z.string().optional(),
  //     name: z.string(),
  //   })
  // ),
});

export type PlanFormValues = z.infer<typeof planSchema>;

export const usePlanForm = (plan?: PlanEditor_plan$data) => {
  const form = useForm<PlanFormValues>({
    resolver: zodResolver(planSchema),
    defaultValues: createFormData(plan ?? ({} as PlanEditor_plan$data)),
  });

  return form;
};

export default usePlanForm;
