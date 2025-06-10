import graphql from "babel-plugin-relay/macro";

import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import * as z from "zod";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { useFragment, useMutation } from "react-relay";
import type {
  DiscountCodeEditor_discountCode$key,
  DiscountCodeEditor_discountCode$data,
} from "./__generated__/DiscountCodeEditor_discountCode.graphql";
import { useEffect, useState } from "react";
import type {
  DiscountCodeEditorCreateDiscountMutation,
  DiscountCodeEditorCreateDiscountMutation$variables,
} from "./__generated__/DiscountCodeEditorCreateDiscountMutation.graphql";
import ErrorDialog from "@/components/ErrorDialog";
import {
  DiscountCodeEditorUpdateDiscountMutation$variables,
  DiscountCodeEditorUpdateDiscountMutation,
} from "./__generated__/DiscountCodeEditorUpdateDiscountMutation.graphql";
import { useLocation } from "wouter";
import DiscountForm, { DiscountFormProps } from "./DiscountCodeForm";
import { Form } from "@/components/ui/form";
import {
  DiscountCodeEditorDeleteDiscountCodeMutation,
  DiscountCodeEditorDeleteDiscountCodeMutation$variables,
} from "./__generated__/DiscountCodeEditorDeleteDiscountCodeMutation.graphql";
import { Button } from "@/components/ui/button";
import { Trash2 } from "lucide-react";
import { useSnackbar } from "@/components/Snackbar";
import { useRelayScreenContext } from "@/Router/withRelay";
import type { DiscountCodeQuery } from "./__generated__/DiscountCodeQuery.graphql";

const discountSchema = z.object({
  id: z.string().optional(),
  name: z.string().min(1, "Name is required").max(100),
  description: z.string(),
  code: z.string().max(1000).or(z.literal("")),
  priceTierId: z.string(),
  discount: z.number().min(0),
  duration: z.union([
    z.literal("P7D"),
    z.literal("P30D"),
    z.literal("P365D"),
    z.literal("%future added value"),
  ]),
  expiryDate: z.date(),
});

type Props = Readonly<{
  discountCode?: DiscountCodeEditor_discountCode$key;
}>;

export type DiscountCodeFormValues = z.infer<typeof discountSchema>;

function createFormData(
  data: DiscountCodeEditor_discountCode$data | undefined | null
): DiscountCodeFormValues {
  return {
    ...(data ?? {}),
    id: data?.id,
    code: data?.code ?? "",
    description: data?.description ?? "",
    name: data?.name ?? "",
    discount: data?.discount,
    duration: data?.duration ?? "P7D",
    expiryDate: data?.expiryDate,
    priceTierId: data?.priceTierId ?? "",
  };
}

export default function DiscountCodeEditor({ discountCode }: Props) {
  const [, navTo] = useLocation();
  const { showSnackbar } = useSnackbar();
  const { refresh } = useRelayScreenContext<DiscountCodeQuery>();
  const data = useFragment(
    graphql`
      fragment DiscountCodeEditor_discountCode on DiscountCode {
        id
        name
        code
        priceTierId
        description
        discount
        duration
        expiryDate
      }
    `,
    discountCode
  );

  const form = useForm<DiscountCodeFormValues>({
    resolver: zodResolver(discountSchema),
    defaultValues: createFormData(data),
  });

  useEffect(() => {
    // This is so that when the subscription updates the discount, we update the form
    form.reset(createFormData(data), {
      keepDirtyValues: true,
    });
  }, [data, form]);

  const [createDiscountCodeErrors, setCreateDiscountCodeErrors] = useState<
    Error[]
  >([]);
  const [createDiscountCodeMutation] =
    useMutation<DiscountCodeEditorCreateDiscountMutation>(
      graphql`
        mutation DiscountCodeEditorCreateDiscountMutation(
          $createDiscountCodeInput: CreateDiscountCodeInput!
        ) {
          createDiscountCode(input: $createDiscountCodeInput) {
            discountCode {
              id
              ...DiscountCodeEditor_discountCode
            }
          }
        }
      `
    );

  const createDiscountCode = (
    createDiscountCodeInput: DiscountCodeEditorCreateDiscountMutation$variables["createDiscountCodeInput"]
  ) => {
    createDiscountCodeMutation({
      variables: {
        createDiscountCodeInput,
      },
      onError(error) {
        setCreateDiscountCodeErrors([error]);
      },
      onCompleted({ createDiscountCode }) {
        if (createDiscountCode?.discountCode?.id) {
          navTo(
            `/discount-code/${encodeURIComponent(
              createDiscountCode.discountCode.id
            )}`,
            {
              replace: true,
            }
          );
          refresh({ id: createDiscountCode.discountCode.id });
          showSnackbar("success", "Discount created", 3000);
        }
      },
    });
  };

  const [updateDiscountCodeErrors, setUpdateDiscountCodeErrors] = useState<
    Error[]
  >([]);
  const [updateDiscountCodeMutation] =
    useMutation<DiscountCodeEditorUpdateDiscountMutation>(
      graphql`
        mutation DiscountCodeEditorUpdateDiscountMutation(
          $updateDiscountCodeInput: UpdateDiscountCodeInput!
        ) {
          updateDiscountCode(input: $updateDiscountCodeInput) {
            discountCode {
              id
              ...DiscountCodeEditor_discountCode
            }
          }
        }
      `
    );

  const updateDiscountCode = (
    updateDiscountCodeInput: DiscountCodeEditorUpdateDiscountMutation$variables["updateDiscountCodeInput"]
  ) => {
    updateDiscountCodeMutation({
      variables: {
        updateDiscountCodeInput,
      },
      onError(error) {
        setUpdateDiscountCodeErrors([error]);
      },
      onCompleted() {
        showSnackbar("success", "Discount updated", 3000);
      },
    });
  };

  const [deleteDiscountCodeErrors, setDeleteDiscountCodeErrors] = useState<
    Error[]
  >([]);
  const [deleteDiscountCodeMutation] =
    useMutation<DiscountCodeEditorDeleteDiscountCodeMutation>(
      graphql`
        mutation DiscountCodeEditorDeleteDiscountCodeMutation(
          $deleteDiscountCodeInput: DeleteDiscountCodeInput!
        ) {
          deleteDiscountCode(input: $deleteDiscountCodeInput) {
            discountCode {
              id
              ...DiscountCodeEditor_discountCode
            }
          }
        }
      `
    );

  const deleteDiscountCode = (
    deleteDiscountCodeInput: DiscountCodeEditorDeleteDiscountCodeMutation$variables["deleteDiscountCodeInput"]
  ) => {
    deleteDiscountCodeMutation({
      variables: {
        deleteDiscountCodeInput,
      },
      onError(error) {
        setDeleteDiscountCodeErrors([error]);
      },
      onCompleted() {
        navTo("/discounts");
        showSnackbar("success", "Discount deleted", 3000);
      },
    });
  };

  function onSubmitCreate(formData: DiscountCodeFormValues) {
    createDiscountCode(formData);
  }

  function onSubmitUpdate(formData: DiscountCodeFormValues) {
    updateDiscountCode({
      ...formData,
      id: formData.id!,
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
                deleteDiscountCode({ id: id! });
              }}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          </div>
        )}
        <CardHeader>
          <CardTitle>
            {id ? "Edit" : "Create Discount Code"} {form.watch("name")}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <Form {...form}>
            {id ? (
              <EditDiscountForm id={id} form={form} onSubmit={onSubmitUpdate} />
            ) : (
              <DiscountForm form={form} onSubmit={onSubmitCreate} />
            )}
          </Form>
        </CardContent>
      </Card>
      {createDiscountCodeErrors.length > 0 && (
        <ErrorDialog
          error={createDiscountCodeErrors[0]}
          startOpen={createDiscountCodeErrors.length > 0}
          title="Create Error"
          onClose={() => setCreateDiscountCodeErrors([])}
        />
      )}
      {updateDiscountCodeErrors.length > 0 && (
        <ErrorDialog
          error={updateDiscountCodeErrors[0]}
          startOpen={updateDiscountCodeErrors.length > 0}
          title="Update Error"
          onClose={() => setUpdateDiscountCodeErrors([])}
        />
      )}
      {deleteDiscountCodeErrors.length > 0 && (
        <ErrorDialog
          error={deleteDiscountCodeErrors[0]}
          startOpen={deleteDiscountCodeErrors.length > 0}
          title="Delete Error"
          onClose={() => setDeleteDiscountCodeErrors([])}
        />
      )}
    </div>
  );
}

const EditDiscountForm = (props: DiscountFormProps & { id: string }) => {
  return (
    <>
      <DiscountForm {...props} />
    </>
  );
};
