import { Button } from "@/components/ui/button";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card";
import {
  FormField,
  FormItem,
  FormLabel,
  FormControl,
  FormMessage,
  FormDescription,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectTrigger,
  SelectValue,
  SelectContent,
  SelectItem,
} from "@/components/ui/select";
import { Separator } from "@/components/ui/separator";
import { UseFormReturn } from "react-hook-form";
import { Switch } from "@/components/ui/switch";
import { PlanFormValues } from "./PlanEditor";
import { PowerOff, Power, Trash2 } from "lucide-react";
import TextInput from "@/components/TextInput";
// import { DevTool } from "@hookform/devtools";
import { usePriceTierUpdatedSubscription } from "./hooks/PriceTierUpdated";
import { useState } from "react";
import ErrorDialog from "@/components/ErrorDialog";
import {
  PlanFormDeletePriceTierMutation,
  PlanFormDeletePriceTierMutation$variables,
} from "./__generated__/PlanFormDeletePriceTierMutation.graphql";
import { useMutation } from "react-relay";
import { useSnackbar } from "@/components/Snackbar";
import {
  PlanFormPatchPriceTierMutation,
  PlanFormPatchPriceTierMutation$variables,
} from "./__generated__/PlanFormPatchPriceTierMutation.graphql";
import PlanFormDeletePriceTier from "@/mutations/PlanFormDeletePriceTier";
import PlanFormPatchPriceTier from "@/mutations/PlanFormPatchPriceTier";

export type PlanFormProps = {
  form: UseFormReturn<PlanFormValues>;
  onSubmit: (data: PlanFormValues) => void;
};

const PlanForm = ({ form, onSubmit }: PlanFormProps) => {
  return (
    <>
      {/* <DevTool control={form.control} /> */}
      <form
        onSubmit={form.handleSubmit(onSubmit)}
        className="space-y-8"
        autoComplete="off"
      >
        <input type="hidden" {...form.register("id")} />
        <input type="hidden" {...form.register("features.planId")} />
        <TextInput name={`name`} label="Name" placeholder="Enter plan name" />
        <TextInput
          name={`description`}
          label="Description"
          placeholder="Enter plan description"
        />
        <TextInput
          name={`iconUrl`}
          label="Icon URL"
          placeholder="Enter icon URL"
        />
        <FormField
          control={form.control}
          name="isActive"
          render={({ field }) => (
            <FormItem className="flex flex-row items-center justify-between rounded-lg border p-4">
              <div className="space-y-0.5">
                <FormLabel className="text-base">Active</FormLabel>
                <FormDescription>
                  Is this plan currently active?
                </FormDescription>
              </div>
              <FormControl>
                <Switch
                  checked={field.value}
                  onCheckedChange={(e) => {
                    field.onChange(e);
                    if (form.getValues("id")) {
                      form.handleSubmit(onSubmit)();
                    }
                  }}
                />
              </FormControl>
            </FormItem>
          )}
        />
        <TextInput
          name={`foreignServiceId`}
          label="Foreign Service ID"
          placeholder="Pending..."
          readOnly={true}
          description="ID of the plan in the foreign service like Stripe"
        />

        <Separator />

        <div>
          <h3 className="text-lg font-medium">Plan Features</h3>
          <div className="space-y-4 mt-4">
            <FormField
              control={form.control}
              name="features.maxMessages"
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
              name={`features.supportTier`}
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Support Tier</FormLabel>
                  <Select
                    onValueChange={field.onChange}
                    defaultValue={field.value}
                  >
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder="Select a Support Tier" />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      <SelectItem value="BASIC">Basic</SelectItem>
                      <SelectItem value="STANDARD">Standard</SelectItem>
                      <SelectItem value="PREMIUM">Premium</SelectItem>
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="features.aiSupport"
              render={({ field }) => (
                <FormItem className="flex flex-row items-center justify-between rounded-lg border p-4">
                  <div className="space-y-0.5">
                    <FormLabel className="text-base">AI Support</FormLabel>
                    <FormDescription>
                      Does this plan include AI support?
                    </FormDescription>
                  </div>
                  <FormControl>
                    <Switch
                      checked={field.value}
                      onCheckedChange={field.onChange}
                    />
                  </FormControl>
                </FormItem>
              )}
            />
          </div>
        </div>

        <Separator />

        <div>
          <h3 className="text-lg font-medium">Price Tiers</h3>
          <div className="space-y-4 mt-4">
            {form
              .watch("priceTiers")
              .map(({ id, planId }, index) =>
                planId && id ? (
                  <EditPriceTierInputs
                    key={id || index}
                    index={index}
                    form={form}
                    id={id}
                  />
                ) : (
                  <PriceTierInputs
                    key={id || index}
                    index={index}
                    form={form}
                  />
                )
              )}
            <Button
              type="button"
              variant="outline"
              onClick={createNewTier(form)}
              disabled={form.formState.isSubmitting}
            >
              Add Price Tier
            </Button>
          </div>
        </div>

        <Button disabled={form.formState.isSubmitting} type="submit">
          Save Changes
        </Button>
      </form>
      {/* <DevTool control={form.control} /> */}
    </>
  );
};

type PriceTierInputsProps = {
  form: UseFormReturn<PlanFormValues>;
  index: number;
};

const PriceTierInputs = ({ form, index }: PriceTierInputsProps) => {
  const { showSnackbar } = useSnackbar();
  const [deletePriceTierErrors, setDeletePriceTierErrors] = useState<Error[]>(
    []
  );
  const [deletePriceTierMutation] =
    useMutation<PlanFormDeletePriceTierMutation>(PlanFormDeletePriceTier);

  const deletePriceTier = (
    deletePriceTierInput: PlanFormDeletePriceTierMutation$variables["deletePriceTierInput"]
  ) => {
    deletePriceTierMutation({
      variables: {
        deletePriceTierInput,
      },
      onError(error) {
        setDeletePriceTierErrors([error]);
      },
      onCompleted() {
        showSnackbar("success", `Price Tier deleted`, 3000);
      },
    });
  };
  const [patchPriceTierErrors, setPatchPriceTierErrors] = useState<Error[]>([]);
  const [patchPriceTierMutation] = useMutation<PlanFormPatchPriceTierMutation>(
    PlanFormPatchPriceTier
  );

  const patchPriceTier = (
    patchPriceTierInput: PlanFormPatchPriceTierMutation$variables["patchPriceTierInput"]
  ) => {
    patchPriceTierMutation({
      variables: {
        patchPriceTierInput,
      },
      onError(error) {
        setPatchPriceTierErrors([error]);
      },
      onCompleted() {
        showSnackbar("success", "Plan patchd", 3000);
      },
    });
  };
  const id = form.watch(`priceTiers.${index}.id`);
  const status = form.watch(`priceTiers.${index}.status`);
  const inactive = status !== "ACTIVE";
  const name = form.watch(`priceTiers.${index}.name`);
  return (
    <>
      <input type="hidden" {...form.register(`priceTiers.${index}.id`)} />
      <input type="hidden" {...form.register(`priceTiers.${index}.planId`)} />
      <Card key={index}>
        <CardHeader className="flex-row justify-between">
          <CardTitle className="text-lg">
            {name || `Price Tier ${index + 1}`}
          </CardTitle>
          <Button
            type="button"
            size={"sm"}
            variant={id && inactive ? "default" : "destructive"}
            onClick={() => {
              if (!id) {
                return form.setValue(
                  "priceTiers",
                  form
                    .getValues("priceTiers")
                    .filter((_: unknown, i: number) => i !== index)
                );
              }
              if (id && inactive) {
                return patchPriceTier({
                  id: id!,
                  status: "ACTIVE",
                });
              }
              if (id && !inactive) {
                return deletePriceTier({
                  id: id!,
                });
              }
            }}
          >
            {id ? (
              inactive ? (
                <Power className="h-4 w-4" />
              ) : (
                <PowerOff className="h-4 w-4" />
              )
            ) : (
              <Trash2 className="h-4 w-4" />
            )}
          </Button>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <TextInput
              disabled={inactive}
              name={`priceTiers.${index}.name`}
              label="Name"
              placeholder="Name"
            />
            <FormField
              disabled={inactive}
              control={form.control}
              name={`priceTiers.${index}.price`}
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Price</FormLabel>
                  <FormControl>
                    <Input
                      type="number"
                      {...field}
                      onChange={(e) => {
                        if (e.target.value) {
                          field.onChange(parseFloat(e.target.value));
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
              name={`priceTiers.${index}.duration`}
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Duration</FormLabel>
                  <Select
                    onValueChange={field.onChange}
                    defaultValue={field.value}
                    disabled={inactive}
                  >
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
            <FormField
              control={form.control}
              name={`priceTiers.${index}.status`}
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Status</FormLabel>
                  <Select
                    onValueChange={field.onChange}
                    defaultValue={field.value}
                    disabled={true}
                  >
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder="Select a Status" />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      <SelectItem value="ACTIVE">Active</SelectItem>
                      <SelectItem value="INACTIVE">Inactive</SelectItem>
                      <SelectItem disabled={true} value="ARCHIVED">
                        Archived
                      </SelectItem>
                      <SelectItem disabled={true} value="DELETED">
                        Deleted
                      </SelectItem>
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />
            <TextInput
              name={`priceTiers.${index}.description`}
              label="Description"
              placeholder="Description"
              disabled={inactive}
            />
            <TextInput
              name={`priceTiers.${index}.iconUrl`}
              label="Icon URL"
              placeholder="Enter icon URL"
              disabled={inactive}
            />
            <TextInput
              name={`priceTiers.${index}.foreignServiceId`}
              label="Foreign Service ID"
              placeholder="Pending..."
              readOnly={true}
              disabled={inactive}
            />
          </div>
        </CardContent>
      </Card>

      {deletePriceTierErrors.length > 0 && (
        <ErrorDialog
          error={deletePriceTierErrors[0]}
          startOpen={deletePriceTierErrors.length > 0}
          title="Delete Error"
          onClose={() => setDeletePriceTierErrors([])}
        />
      )}

      {patchPriceTierErrors.length > 0 && (
        <ErrorDialog
          error={patchPriceTierErrors[0]}
          startOpen={patchPriceTierErrors.length > 0}
          title="Delete Error"
          onClose={() => setPatchPriceTierErrors([])}
        />
      )}
    </>
  );
};

const EditPriceTierInputs = ({
  id,
  ...props
}: PriceTierInputsProps & { id: string }) => {
  const [priceTierUpdatedError, setPriceTierUpdatedError] =
    useState<Error | null>(null);
  usePriceTierUpdatedSubscription(id, (e) => setPriceTierUpdatedError(e));

  return (
    <>
      <PriceTierInputs {...props} />
      {!!priceTierUpdatedError && (
        <ErrorDialog
          error={priceTierUpdatedError}
          startOpen={!!priceTierUpdatedError}
          title="Subscription Error"
          onClose={() => setPriceTierUpdatedError(null)}
        />
      )}
    </>
  );
};

function createNewTier(form: UseFormReturn<PlanFormValues>) {
  return () => {
    const tiers = form.getValues("priceTiers");
    form.setValue("priceTiers", [
      ...tiers,
      {
        price: 0,
        duration: "P7D",
        name: "",
        description: "",
        iconUrl: "",
        foreignServiceId: "",
        planId: form.getValues("id"),
        status: "ACTIVE",
      },
    ]);
  };
}

export default PlanForm;
