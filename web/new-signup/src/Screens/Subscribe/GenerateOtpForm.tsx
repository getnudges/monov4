import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { Form } from "@/components/ui/form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import parsePhoneNumber from "libphonenumber-js";
import PhoneNumberInput from "@/components/PhoneNumberInput";

const FormSchema = z.object({
  clientId: z.string().readonly(),
  phoneNumber: z
    .string({
      message: "Please enter a valid phone number.",
    })
    .transform((value, ctx) => {
      const phoneNumber = parsePhoneNumber(value, {
        defaultCountry: "US",
      });

      if (!phoneNumber?.isValid()) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: "Invalid phone number",
        });
        return z.NEVER;
      }

      return phoneNumber.formatInternational();
    }),
});

export type FormDataType = z.infer<typeof FormSchema>;

type GenerateOtpFormProps = Readonly<{
  onSubmit: (input: FormDataType) => void;
}>;

export default function GenerateOtpForm({
  onSubmit,
}: Readonly<GenerateOtpFormProps>) {
  const form = useForm<FormDataType>({
    resolver: zodResolver(FormSchema),
    defaultValues: {
      clientId: "",
      phoneNumber: "",
    },
  });

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)}>
        <input type="hidden" name="clientId" />
        <PhoneNumberInput name="phoneNumber" label="Your Phone Number" />
        <Button type="submit" disabled={form.formState.isSubmitting}>
          Get OTP
        </Button>
      </form>
    </Form>
  );
}
