import { useFormContext } from "react-hook-form";
import { type PlanFormValues } from "../usePlanForm";
import TextInput from "@/components/TextInput";
import { Form } from "radix-ui";

export type CreatePlanFormProps = {
  onSubmit: (data: PlanFormValues) => void;
};

const CreatePlanForm = ({ onSubmit }: CreatePlanFormProps) => {
  const form = useFormContext<PlanFormValues>();
  return (
    <>
      <Form.Root onSubmit={form.handleSubmit(onSubmit)} autoComplete="off">
        <input type="hidden" {...form.register("id")} />
        <input type="hidden" {...form.register("features.planId")} />
        <TextInput name={`name`} label="Name" placeholder="Enter plan name" />
        <TextInput
          name={`description`}
          label="Description"
          placeholder="Enter plan description"
        />
        <input type="submit" value="Create Plan" />
      </Form.Root>
    </>
  );
};

export default CreatePlanForm;
