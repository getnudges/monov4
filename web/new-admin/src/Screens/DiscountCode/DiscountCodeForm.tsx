import { Button } from "@/components/ui/button";
import { UseFormReturn } from "react-hook-form";
import { DiscountCodeFormValues } from "./DiscountCodeEditor";
import TextInput from "@/components/TextInput";
import {
  FormField,
  FormItem,
  FormLabel,
  FormControl,
  FormMessage,
} from "@/components/ui/form";
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from "@radix-ui/react-select";
import { Input } from "@/components/ui/input";

export type DiscountFormProps = {
  form: UseFormReturn<DiscountCodeFormValues>;
  onSubmit: (data: DiscountCodeFormValues) => void;
};

const DiscountCodeForm = ({ form, onSubmit }: DiscountFormProps) => {
  return (
    <>
      <form
        onSubmit={form.handleSubmit(onSubmit)}
        className="space-y-8"
        autoComplete="off"
      >
        <input type="hidden" {...form.register("id")} />
        <TextInput
          name={`name`}
          label="Name"
          placeholder="Enter a friendly name"
        />
        <TextInput
          name={`discountCode`}
          label="Code"
          placeholder="Enter user-facing code"
        />
        <input type="hidden" {...form.register("priceTierId")} />
        {/* TODO: I need a dialog that pulls up the price tiers */}
        <TextInput
          name={`description`}
          label="Description"
          placeholder="Enter plan description"
        />
        <FormField
          control={form.control}
          name="discount"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Max Messages</FormLabel>
              <FormControl>
                <Input
                  type="number"
                  {...field}
                  onChange={(e) => {
                    if (e.target.value) {
                      field.onChange(parseInt(e.target.value));
                    } else {
                      field.onChange("");
                    }
                  }}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name={`duration`}
          render={({ field }) => (
            <FormItem>
              <FormLabel>Duration</FormLabel>
              <Select onValueChange={field.onChange} defaultValue={field.value}>
                <FormControl>
                  <SelectTrigger>
                    <SelectValue placeholder="Select a Duration" />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  <SelectItem value="P7D">Weekly</SelectItem>
                  <SelectItem value="P30D">Monthly</SelectItem>
                  <SelectItem value="P365D">Yearly</SelectItem>
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />
        <TextInput
          name={`expiryDate`}
          label="Expiration Date"
          placeholder="Enter user-facing code"
        />

        <Button disabled={form.formState.isSubmitting} type="submit">
          Save Changes
        </Button>
      </form>
    </>
  );
};

export default DiscountCodeForm;
