import graphql from "babel-plugin-relay/macro";

import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import * as z from "zod";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useFragment, useMutation } from "react-relay";
import type {
  PlanEditor_plan$key,
  PlanEditor_plan$data,
} from "./__generated__/PlanEditor_plan.graphql";
import { useEffect, useState } from "react";
import type {
  PlanEditorCreatePlanMutation,
  PlanEditorCreatePlanMutation$variables,
} from "./__generated__/PlanEditorCreatePlanMutation.graphql";
import ErrorDialog from "@/components/ErrorDialog";
import {
  PlanEditorUpdatePlanMutation$variables,
  PlanEditorUpdatePlanMutation,
} from "./__generated__/PlanEditorUpdatePlanMutation.graphql";
import { usePlanUpdatedSubscription } from "./hooks/PlanUpdated";
import { useLocation } from "wouter";
import PlanForm, { PlanFormProps } from "./PlanForm";
import { Form } from "@/components/ui/form";
import {
  PlanEditorDeletePlanMutation,
  PlanEditorDeletePlanMutation$variables,
} from "./__generated__/PlanEditorDeletePlanMutation.graphql";
import { Button } from "@/components/ui/button";
import { Trash2 } from "lucide-react";
import { useSnackbar } from "@/components/Snackbar";
import { useRelayScreenContext } from "@/Router/withRelay";
import type { PlanQuery } from "./__generated__/PlanQuery.graphql";

const planSchema = z.object({
  id: z.string().optional(),
  name: z.string().min(1, "Name is required").max(100),
  description: z.string(),
  iconUrl: z.string().max(1000).url("Invalid URL").optional().or(z.literal("")),
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

type Props = Readonly<{
  plan?: PlanEditor_plan$key;
}>;

export type PlanFormValues = z.infer<typeof planSchema>;

function createFormData(
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

export default function PlanEditor({ plan }: Props) {
  const [, navTo] = useLocation();
  const { showSnackbar } = useSnackbar();
  const { refresh } = useRelayScreenContext<PlanQuery>();
  const data = useFragment(
    graphql`
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
    `,
    plan
  );

  const form = useForm<PlanFormValues>({
    resolver: zodResolver(planSchema),
    defaultValues: createFormData(data),
  });

  useEffect(() => {
    // This is so that when the subscription updates the plan, we update the form
    form.reset(createFormData(data), {
      keepDirtyValues: true,
    });
  }, [data, form]);

  const [createPlanErrors, setCreatePlanErrors] = useState<Error[]>([]);
  const [createPlanMutation] = useMutation<PlanEditorCreatePlanMutation>(
    graphql`
      mutation PlanEditorCreatePlanMutation(
        $createPlanInput: CreatePlanInput!
      ) {
        createPlan(input: $createPlanInput) {
          plan {
            id
            ...PlanEditor_plan
          }
        }
      }
    `
  );

  const createPlan = (
    createPlanInput: PlanEditorCreatePlanMutation$variables["createPlanInput"]
  ) => {
    createPlanMutation({
      variables: {
        createPlanInput,
      },
      onError(error) {
        setCreatePlanErrors([error]);
      },
      onCompleted({ createPlan }) {
        if (createPlan?.plan?.id) {
          showSnackbar("success", "Plan created", 3000);
          navTo(`/plan/${encodeURIComponent(createPlan.plan.id)}`, {
            replace: true,
          });
          refresh({ id: createPlan.plan.id });
        }
      },
    });
  };

  const [updatePlanErrors, setUpdatePlanErrors] = useState<Error[]>([]);
  const [updatePlanMutation] = useMutation<PlanEditorUpdatePlanMutation>(
    graphql`
      mutation PlanEditorUpdatePlanMutation(
        $updatePlanInput: UpdatePlanInput!
      ) {
        updatePlan(input: $updatePlanInput) {
          plan {
            id
            ...PlanEditor_plan
          }
        }
      }
    `
  );

  const updatePlan = (
    updatePlanInput: PlanEditorUpdatePlanMutation$variables["updatePlanInput"]
  ) => {
    updatePlanMutation({
      variables: {
        updatePlanInput,
      },
      onError(error) {
        setUpdatePlanErrors([error]);
      },
      onCompleted() {
        showSnackbar("success", "Plan updated", 3000);
      },
    });
  };

  const [deletePlanErrors, setDeletePlanErrors] = useState<Error[]>([]);
  const [deletePlanMutation] = useMutation<PlanEditorDeletePlanMutation>(
    graphql`
      mutation PlanEditorDeletePlanMutation(
        $deletePlanInput: DeletePlanInput!
      ) {
        deletePlan(input: $deletePlanInput) {
          plan {
            id
            ...PlanEditor_plan
          }
        }
      }
    `
  );

  const deletePlan = (
    deletePlanInput: PlanEditorDeletePlanMutation$variables["deletePlanInput"]
  ) => {
    deletePlanMutation({
      variables: {
        deletePlanInput,
      },
      onError(error) {
        setDeletePlanErrors([error]);
      },
      onCompleted() {
        navTo("/plans");
        showSnackbar("success", "Plan deleted", 3000);
      },
    });
  };

  function onSubmitCreate(formData: PlanFormValues) {
    createPlan({
      name: formData.name,
      description: formData.description,
      iconUrl: formData.iconUrl ?? "",
      activateOnCreate: formData.isActive ?? false,
      features: {
        maxMessages: formData.features.maxMessages,
        supportTier: formData.features.supportTier,
        aiSupport: formData.features.aiSupport,
      },
      priceTiers: formData.priceTiers?.map((tier) => ({
        name: tier.name,
        price: tier.price,
        duration: tier.duration,
        description: tier.description,
        iconUrl: tier.iconUrl,
        planId: tier.planId || undefined,
        // status: tier.status || "ACTIVE",
      })),
    });
  }

  function onSubmitUpdate(formData: PlanFormValues) {
    updatePlan({
      id: formData.id!,
      name: formData.name,
      description: formData.description,
      iconUrl: formData.iconUrl,
      isActive: formData.isActive,
      features: {
        ...formData.features,
        planId: formData.features.planId!,
      },
      foreignServiceId: formData.foreignServiceId,
      priceTiers: formData.priceTiers?.map((tier) => ({
        name: tier.name,
        price: tier.price,
        duration: tier.duration,
        description: tier.description,
        iconUrl: tier.iconUrl,
        planId: tier.planId!,
        id: tier.id || undefined,
        status: tier.status || "ACTIVE",
      })),
    });
  }

  const id = form.watch("id");
  return (
    <div className="container mx-auto py-10">
      <Card className="relative">
        {id && (
          <div className="absolute top-4 right-4 w-8 h-8">
            <Button
              type="button"
              size={"sm"}
              variant="destructive"
              onClick={() => {
                deletePlan({ id: id! });
              }}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          </div>
        )}
        <CardHeader>
          <CardTitle>
            {id ? "Edit" : "Create Plan"} {form.watch("name")}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <Form {...form}>
            {id ? (
              <EditPlanForm id={id} form={form} onSubmit={onSubmitUpdate} />
            ) : (
              <PlanForm form={form} onSubmit={onSubmitCreate} />
            )}
          </Form>
        </CardContent>
      </Card>
      {createPlanErrors.length > 0 && (
        <ErrorDialog
          error={createPlanErrors[0]}
          startOpen={createPlanErrors.length > 0}
          title="Create Error"
          onClose={() => setCreatePlanErrors([])}
        />
      )}
      {updatePlanErrors.length > 0 && (
        <ErrorDialog
          error={updatePlanErrors[0]}
          startOpen={updatePlanErrors.length > 0}
          title="Update Error"
          onClose={() => setUpdatePlanErrors([])}
        />
      )}
      {deletePlanErrors.length > 0 && (
        <ErrorDialog
          error={deletePlanErrors[0]}
          startOpen={deletePlanErrors.length > 0}
          title="Delete Error"
          onClose={() => setDeletePlanErrors([])}
        />
      )}
    </div>
  );
}

const EditPlanForm = ({ id, ...props }: PlanFormProps & { id: string }) => {
  const [planUpdatedError, setPlanUpdatedError] = useState<Error | null>(null);
  usePlanUpdatedSubscription(id, (e) => setPlanUpdatedError(e));

  return (
    <>
      <PlanForm {...props} />
      {!!planUpdatedError && (
        <ErrorDialog
          error={planUpdatedError}
          startOpen={!!planUpdatedError}
          title="Subscription Error"
          onClose={() => setPlanUpdatedError(null)}
        />
      )}
    </>
  );
};

