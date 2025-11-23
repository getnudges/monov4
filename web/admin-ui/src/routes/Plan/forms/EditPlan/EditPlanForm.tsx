import { useFormContext } from "react-hook-form";
import TextInput from "@/components/TextInput";
import { Form } from "radix-ui";
import type { PlanFormValues } from "../usePlanForm";

export type EditPlanFormProps = Readonly<{
  onSubmit: (data: PlanFormValues) => void;
  onDelete: (args: { id: string }) => void;
}>;

const EditPlanForm = ({ onSubmit, onDelete }: EditPlanFormProps) => {
  const form = useFormContext<PlanFormValues>();

  const id = form.getValues("id");

  const handleDelete = () => {
    onDelete({ id: id! });
  };

  return (
    <>
      <Form.Root onSubmit={form.handleSubmit(onSubmit)} autoComplete="off">
        <button type="button" onClick={handleDelete}>
          Delete Plan
        </button>
        <input type="hidden" {...form.register("id")} />
        <input type="hidden" {...form.register("features.planId")} />
        <TextInput name={`name`} label="Name" placeholder="Enter plan name" />
        <TextInput
          name={`description`}
          label="Description"
          placeholder="Enter plan description"
        />
        <input type="submit" value="Update Plan" />
      </Form.Root>
    </>
  );
};

export default EditPlanForm;
