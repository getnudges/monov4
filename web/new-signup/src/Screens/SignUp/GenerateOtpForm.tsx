import { useForm } from "react-hook-form";
import TextInput from "@/components/TextInput";
import { Button } from "@/components/ui/button";
import { Form } from "@/components/ui/form";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import parsePhoneNumber from "libphonenumber-js";
import PhoneNumberInput from "@/components/PhoneNumberInput";

const FormSchema = z.object({
  name: z.string().min(6, {
    message: "Your business name must be at least 6 characters.",
  }),
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

type GenerateOtpFormProps = Readonly<{
  onSubmit: (input: z.infer<typeof FormSchema>) => void;
}>;

export default function GenerateOtpForm({
  onSubmit,
}: Readonly<GenerateOtpFormProps>) {
  const form = useForm<z.infer<typeof FormSchema>>({
    resolver: zodResolver(FormSchema),
    defaultValues: {
      name: "",
      phoneNumber: "",
    },
  });

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)}>
        <TextInput
          name="name"
          placeholder="My Company, LLC"
          label="Your Business Name"
        />
        <PhoneNumberInput name="phoneNumber" label="Phone Number" />
        <Button type="submit" disabled={form.formState.isSubmitting}>
          Get OTP
        </Button>
      </form>
    </Form>
  );
}
