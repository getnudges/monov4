import { FormProvider } from "react-hook-form";
import { useLocation } from "wouter";
import { useState } from "react";
import { useFragment, useMutation } from "react-relay";
import type {
  PlanEditor_plan$data,
  PlanEditor_plan$key,
} from "@/fragments/__generated__/PlanEditor_plan.graphql";
import PlanEditorUpdatePlan from "@/mutations/PlanEditorUpdatePlan";
import type {
  PlanEditorUpdatePlanMutation,
  PlanEditorUpdatePlanMutation$variables,
} from "@/mutations/__generated__/PlanEditorUpdatePlanMutation.graphql";
import Alert from "@/components/AlertDialog";
import { usePlanUpdatedSubscription } from "@/subscriptions/PlanUpdated";
import EditPlanForm from "./EditPlanForm";
import { useToaster } from "@/components/Toaster";
import type { GraphQLSubscriptionError } from "@/types";
import type {
  PlanEditorDeletePlanMutation,
  PlanEditorDeletePlanMutation$variables,
} from "@/mutations/__generated__/PlanEditorDeletePlanMutation.graphql";
import PlanEditorDeletePlan from "@/mutations/PlanEditorDeletePlan";
import { usePlanForm, type PlanFormValues } from "../usePlanForm";
import PlanEditor_plan from "@/fragments/PlanEditor";

type PlanEditorProps = Readonly<{
  plan: PlanEditor_plan$key;
}>;

export default function PlanEditor({ plan }: PlanEditorProps) {
  const [, navTo] = useLocation();
  const { notify } = useToaster();

  const data = useFragment(PlanEditor_plan, plan);
  const [planUpdatedErrors, setPlanUpdatedErrors] = useState<
    GraphQLSubscriptionError[]
  >([]);
  usePlanUpdatedSubscription(data.id, (e) => setPlanUpdatedErrors(e));

  const form = usePlanForm(data);

  const [updatePlanErrors, setUpdatePlanErrors] = useState<Error[]>([]);
  const [updatePlanMutation] =
    useMutation<PlanEditorUpdatePlanMutation>(PlanEditorUpdatePlan);

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
        notify("success", "Plan updated", 3000);
      },
    });
  };

  const [deletePlanErrors, setDeletePlanErrors] = useState<Error[]>([]);
  const [deletePlanMutation] =
    useMutation<PlanEditorDeletePlanMutation>(PlanEditorDeletePlan);

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
        notify("success", "Plan deleted", 3000);
      },
    });
  };

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
        id: tier.id,
        status: tier.status || "ACTIVE",
      })),
    });
  }

  return (
    <>
      <FormProvider {...form}>
        <EditPlanForm onSubmit={onSubmitUpdate} onDelete={deletePlan} />
      </FormProvider>
      {updatePlanErrors.length > 0 && (
        <Alert
          message={updatePlanErrors[0].stack ?? updatePlanErrors[0].message}
          open={updatePlanErrors.length > 0}
          title="Update Error"
          onClose={() => setUpdatePlanErrors([])}
        />
      )}
      {deletePlanErrors.length > 0 && (
        <Alert
          message={deletePlanErrors[0].stack ?? deletePlanErrors[0].message}
          open={deletePlanErrors.length > 0}
          title="Delete Error"
          onClose={() => setDeletePlanErrors([])}
        />
      )}
    </>
  );
}
