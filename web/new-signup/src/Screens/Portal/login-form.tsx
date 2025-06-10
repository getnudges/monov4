import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Link } from "wouter";
import PhoneNumberInput from "@/components/PhoneNumberInput";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import parsePhoneNumber from "libphonenumber-js";
import { z } from "zod";
import { Form } from "@/components/ui/form";

const FormSchema = z.object({
  code: z
    .string()
    .min(6, {
      message: "Your OTP code must be at least 6 characters.",
    })
    .optional(),
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

export type LoginFormData = z.infer<typeof FormSchema>;

type LoginFormProps = Readonly<{
  onSubmit: (input: LoginFormData) => void;
  className?: string;
  disableAction: boolean;
}>;

export function LoginForm({
  className,
  onSubmit,
  disableAction,
}: LoginFormProps) {
  const form = useForm<LoginFormData>({
    resolver: zodResolver(FormSchema),
    defaultValues: {
      phoneNumber: "",
    },
  });

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)}>
        <div className={cn("flex flex-col gap-6", className)}>
          <Card>
            <CardHeader>
              <CardTitle className="text-2xl">Login</CardTitle>
              <CardDescription>
                Enter your phone number below to login to your account.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="flex flex-col gap-6">
                <div className="grid gap-2">
                  <PhoneNumberInput
                    name="phoneNumber"
                    placeholder="Enter your phone number"
                    readOnly={form.formState.isSubmitting}
                  />
                </div>
                <Button
                  disabled={disableAction}
                  type="submit"
                  className="w-full"
                >
                  Login
                </Button>
              </div>
              <div className="mt-4 text-center text-sm">
                Don&apos;t have an account?{" "}
                <Link to="/signup" className="underline underline-offset-4">
                  Sign up
                </Link>
              </div>
            </CardContent>
          </Card>
        </div>
      </form>
    </Form>
  );
}
