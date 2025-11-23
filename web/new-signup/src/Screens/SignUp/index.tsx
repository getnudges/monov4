import type { SignUpQuery } from "./__generated__/SignUpQuery.graphql";
import type {
  SignUp_CreateClientMutation,
  SignUp_CreateClientMutation$variables,
} from "./__generated__/SignUp_CreateClientMutation.graphql";
import type { RelayRoute } from "../../Router/withRelay";
import { useCallback, useState } from "react";
import GenerateOtpForm from "./GenerateOtpForm";
import OtpInputForm from "@/components/OtpInputForm";
import { useLocation } from "wouter";
import {
  useMutation,
  fetchQuery,
  useRelayEnvironment,
} from "react-relay/hooks";
import { Button } from "@/components/ui/button";
import ErrorDialog from "@/components/ErrorDialog";
import Countdown from "@/components/Countdown";
import { OtpInputFormData } from "@/components/OtpInputForm";
import graphql from "babel-plugin-relay/macro";
import { SignUpQueryDef } from "./SignUp";

export const CountdownSeconds = 30;

type GenerateResult = {
  name: string;
  phoneNumber: string;
  code?: string;
};

export default function SignupPage({ data }: RelayRoute<SignUpQuery>) {
  const [, navigate] = useLocation();

  const [generateResult, setGenerateResult] = useState<GenerateResult>({
    name: "",
    phoneNumber: "",
  });

  const [createClientError, setCreateClientError] = useState<Error | null>(
    null
  );
  const [createClientMutation] = useMutation<SignUp_CreateClientMutation>(
    graphql`
      mutation SignUp_CreateClientMutation(
        $createClientInput: CreateClientInput!
      ) {
        createClient(input: $createClientInput) {
          client {
            id
          }
          errors {
            ... on Error {
              message
            }
          }
        }
      }
    `
  );

  const [verifyInFlight, setVerifyInFlight] = useState(false);
  const [verifyError, setVerifyError] = useState<Error | null>(null);

  const relayEnvironment = useRelayEnvironment();

  const getClient = useCallback(() => {
    return fetchQuery<SignUpQuery>(
      relayEnvironment,
      SignUpQueryDef,
      {}
    ).toPromise();
  }, [relayEnvironment]);

  const createClient = useCallback(
    (
      createClientInput: SignUp_CreateClientMutation$variables["createClientInput"]
    ) => {
      return new Promise((resolve, reject) => {
        createClientMutation({
          variables: {
            createClientInput,
          },
          onCompleted: (data) => {
            if (data.createClient?.client?.id) {
              resolve(data.createClient.client.id);
            } else {
              reject(
                new Error(
                  data.createClient.errors?.map((e) => e.message)?.join("\n") ??
                    "Failed to create client"
                )
              );
            }
          },
          onError: (e) => {
            reject(e);
          },
        });
      });
    },
    [createClientMutation]
  );

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

        if (!resp.ok) {
          const data = await resp.json();
          throw new Error(data.message ?? "Failed to validate OTP");
        }

        const clientResult = await getClient();
        if (clientResult?.viewer?.id && clientResult.viewer.subscriptionId) {
          return navigate("/dashboard");
        }
        if (clientResult?.viewer?.id && !clientResult.viewer.subscriptionId) {
          return navigate("/plans");
        }

        await createClient({ name: generateResult.name, locale: "en-US" });
        navigate("/plans");
      } catch (e) {
        setVerifyError(e as Error);
      } finally {
        setVerifyInFlight(false);
      }
    },
    [generateResult, getClient, createClient, navigate]
  );

  const [generateInFlight, setGenerateInFlight] = useState(false);
  const [generateError, setGenerateError] = useState<Error | null>(null);

  const generateOtp = async ({
    name,
    phoneNumber,
  }: {
    name: string;
    phoneNumber: string;
  }) => {
    setGenerateInFlight(true);
    try {
      const phone = `+${phoneNumber.replace(/\D/g, "")}`;
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
      if (!resp.ok) {
        const data = await resp.json();
        throw new Error(data.message ?? "Failed to send OTP");
      }
      setGenerateResult({
        phoneNumber: phone,
        name,
      });
    } catch (e) {
      setGenerateError(e as Error);
    } finally {
      setGenerateInFlight(false);
    }
  };

  return (
    <div>
      <h1>Sign Up</h1>
      <p>Join {data.totalClients} other businesses</p>
      {!generateResult.phoneNumber && (
        <GenerateOtpForm onSubmit={generateOtp} />
      )}
      {generateResult.phoneNumber && (
        <>
          <OtpInputForm onSubmit={verifyOtp} code={generateResult.code} />
          <Countdown
            seconds={CountdownSeconds}
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
      {createClientError && (
        <ErrorDialog
          title="Failed to create account"
          error={createClientError} // TODO: show all errors
          startOpen={!!createClientError}
          onClose={() => setCreateClientError(null)}
        />
      )}
    </div>
  );
}
