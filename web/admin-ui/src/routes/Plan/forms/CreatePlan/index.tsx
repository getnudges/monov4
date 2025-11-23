import { FormProvider } from "react-hook-form";
import { useLocation } from "wouter";
import { useState } from "react";
import { useMutation } from "react-relay";
import { useRelayScreenContext } from "@/Router/withRelay";
import type { PlanQuery } from "../../__generated__/PlanQuery.graphql";
import PlanEditorCreatePlan from "@/mutations/PlanEditorCreatePlan";
import type {
  PlanEditorCreatePlanMutation,
  PlanEditorCreatePlanMutation$variables,
} from "@/mutations/__generated__/PlanEditorCreatePlanMutation.graphql";
import Alert from "@/components/AlertDialog";
import CreatePlanForm from "./CreatePlanForm";
import { useToaster } from "@/components/Toaster";
import { usePlanForm, type PlanFormValues } from "../usePlanForm";

export default function PlanCreator() {
  const [, navTo] = useLocation();
  const { refresh } = useRelayScreenContext<PlanQuery>();
  const { notify } = useToaster();

  const form = usePlanForm();

  const [createPlanErrors, setCreatePlanErrors] = useState<Error[]>([]);
  const [createPlanMutation] =
    useMutation<PlanEditorCreatePlanMutation>(PlanEditorCreatePlan);

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
          notify("success", "Plan created", 3000);
          navTo(`/plan/${encodeURIComponent(createPlan.plan.id)}`, {
            replace: true,
          });
          refresh({ id: createPlan.plan.id });
        }
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
      })),
    });
  }

  return (
    <>
      <FormProvider {...form}>
        <CreatePlanForm onSubmit={onSubmitCreate} />
      </FormProvider>
      {createPlanErrors.length > 0 && (
        <Alert
          message={createPlanErrors[0].stack ?? createPlanErrors[0].message}
          open={createPlanErrors.length > 0}
          title="Create Error"
          onClose={() => setCreatePlanErrors([])}
        />
      )}
    </>
  );
}
