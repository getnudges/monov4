import type { PortalQuery } from "./__generated__/PortalQuery.graphql";
import { useRelayScreenContext, type RelayRoute } from "@/Router/withRelay";
import { Redirect } from "wouter";
import { LoginForm, type LoginFormData } from "./login-form";
import { useCallback, useState } from "react";
import ErrorDialog from "@/components/ErrorDialog";
import OtpInputForm, { type OtpInputFormData } from "@/components/OtpInputForm";
import Countdown from "@/components/Countdown";
import { Button } from "@/components/ui/button";

export default function PortalPage({
  data,
}: Readonly<RelayRoute<PortalQuery>>) {
  const { refresh } = useRelayScreenContext();
  const [generateResult, setGenerateResult] = useState<LoginFormData>({
    phoneNumber: "",
    code: "",
  });

  const [verifyInFlight, setVerifyInFlight] = useState(false);
  const [verifyError, setVerifyError] = useState<Error | null>(null);

  const verifyOtp = useCallback(
    async ({ code }: OtpInputFormData) => {
      setVerifyInFlight(true);
      try {
        const resp = await fetch("/auth/otp/verify", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            "X-Role-Claim": "client",
          },
          body: JSON.stringify({
            phoneNumber: `+${generateResult.phoneNumber.replace(/\D/g, "")}`,
            code,
          }),
        });

        const data = await resp.json();
        if (!resp.ok) {
          throw setVerifyError(new Error(data.message ?? "Failed to send OTP"));
        }
        refresh();
        // TODO: need to verify that the user is a client
      } catch (e) {
        setVerifyError(e as Error);
      } finally {
        setVerifyInFlight(false);
      }
    },
    [generateResult, refresh]
  );

  const [generateInFlight, setGenerateInFlight] = useState(false);
  const [generateError, setGenerateError] = useState<Error | null>(null);

  const generateOtp = useCallback(async ({ phoneNumber }: LoginFormData) => {
    setGenerateInFlight(true);
    try {
      const resp = await fetch("/auth/otp", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "X-Role-Claim": "client",
        },
        body: JSON.stringify({
          phoneNumber: `+${phoneNumber.replace(/\D/g, "")}`,
        }),
      });
      const data = await resp.json();
      if (!resp.ok) {
        throw setGenerateError(new Error(data.message ?? "Failed to send OTP"));
      }
      setGenerateResult({
        phoneNumber: `+${phoneNumber.replace(/\D/g, "")}`,
        code: data?.code,
      });
    } catch (e) {
      setGenerateError(e as Error);
    } finally {
      setGenerateInFlight(false);
    }
  }, []);

  if (data.viewer?.id) {
    return <Redirect to="/dashboard" />;
  }

  return (
    <>
      {!generateResult.phoneNumber && (
        <LoginForm onSubmit={generateOtp} disableAction={generateInFlight} />
      )}
      {generateResult.phoneNumber && (
        <>
          <OtpInputForm
            onSubmit={verifyOtp}
            code={generateResult.code}
            disableAction={verifyInFlight}
          />
          <Countdown
            seconds={30}
            message={(time) => `Resend OTP in ${time} seconds`}
          >
            <Button
              disabled={verifyInFlight || generateInFlight}
              onClick={() => void generateOtp(generateResult)}
              variant={"link"}
              size={"sm"}
            >
              Resend OTP
            </Button>
          </Countdown>
        </>
      )}
      {generateError && (
        <ErrorDialog
          title="Failed to send OTP"
          error={generateError} // TODO: show all errors
          startOpen={!!generateError}
          onClose={() => setGenerateError(null)}
        />
      )}
      {verifyError && (
        <ErrorDialog
          title="Failed to verify OTP"
          error={verifyError} // TODO: show all errors
          startOpen={!!verifyError}
          onClose={() => setVerifyError(null)}
        />
      )}
    </>
  );
}
