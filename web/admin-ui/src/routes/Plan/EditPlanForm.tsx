import { useForm } from "react-hook-form";
import { type PlanFormValues } from "./PlanEditor";
import TextInput from "@/components/TextInput";
import { Form } from "radix-ui";

export type EditPlanFormProps = {
  onSubmit: (data: PlanFormValues) => void;
};

const EditPlanForm = ({ onSubmit }: EditPlanFormProps) => {
  const form = useForm<PlanFormValues>();
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
      </Form.Root>
    </>
  );
};

export default EditPlanForm;
