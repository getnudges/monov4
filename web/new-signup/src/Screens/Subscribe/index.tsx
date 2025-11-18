import graphql from "babel-plugin-relay/macro";

import type { SubscribeQuery } from "./__generated__/SubscribeQuery.graphql";
import type {
  Subscribe_CreateClientMutation,
  Subscribe_CreateClientMutation$variables,
} from "./__generated__/Subscribe_CreateClientMutation.graphql";
import type { RelayRoute } from "../../Router/withRelay";
import { useCallback, useEffect, useState } from "react";
import GenerateOtpForm, { FormDataType } from "./GenerateOtpForm";
import OtpInputForm from "./OtpInputForm";
import { useMutation } from "react-relay/hooks";
import { Button } from "@/components/ui/button";
import ErrorDialog from "@/components/ErrorDialog";

export const SubscribeQueryDef = graphql`
  query SubscribeQuery($slug: String!) {
    clientBySlug(slug: $slug) {
      id
      name
    }
  }
`;

export const CountdownSeconds = 30;

function Countdown({
  seconds,
  children,
  message,
  done,
}: React.PropsWithChildren<{
  seconds: number;
  message: (c: number) => string;
  done?: () => void;
}>) {
  const [count, setCount] = useState(seconds);
  useEffect(() => {
    const interval = setInterval(() => {
      setCount((c) => c - 1);
    }, 1000);
    return () => {
      setCount(seconds);
      done?.();
      clearInterval(interval);
    };
  }, [done, seconds]);
  return <div>{count > 0 ? message(count) : children}</div>;
}

export default function SubscribePage({ data }: RelayRoute<SubscribeQuery>) {
  const [generateResult, setGenerateResult] = useState({
    clientId: data.clientBySlug?.id ?? "",
    phoneNumber: "",
    code: "",
  });
  const [done, setDone] = useState(false);

  const [subscribeToClientError, setCreateClientError] = useState<Error | null>(
    null
  );
  const [subscribeToClientMutation] =
    useMutation<Subscribe_CreateClientMutation>(
      graphql`
        mutation Subscribe_CreateClientMutation(
          $subscribeInput: SubscribeToClientInput!
        ) {
          subscribeToClient(input: $subscribeInput) {
            errors {
              ... on AlreadySubscribedError {
                message
              }
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

  const subscribeToClient = useCallback(
    (
      subscribeInput: Subscribe_CreateClientMutation$variables["subscribeInput"]
    ) => {
      return new Promise((resolve, reject) => {
        subscribeToClientMutation({
          variables: {
            subscribeInput,
          },
          onCompleted: (data) => {
            if (data.subscribeToClient?.errors) {
              reject(
                new Error(
                  data.subscribeToClient?.errors.reduce(
                    (acc, e) => acc + e.message,
                    "\n"
                  )
                )
              );
            } else {
              resolve({});
            }
          },
          onError: (e) => {
            reject(e);
          },
        });
      });
    },
    [subscribeToClientMutation]
  );

  const verifyOtp = useCallback(
    async ({ code }: { code: string }) => {
      setVerifyInFlight(true);
      try {
        const resp = await fetch("/auth/otp", {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            "X-Role-Claim": "subscriber",
          },
          body: JSON.stringify({
            phoneNumber: `+${generateResult.phoneNumber.replace(/\D/g, "")}`,
            code,
          }),
        });
        const data = await resp.json();
        if (!resp.ok) {
          new Error(data.message);
        }
        await subscribeToClient({
          clientId: generateResult.clientId,
        });
        setDone(true);
      } catch (e) {
        setVerifyError(e as Error);
      } finally {
        setVerifyInFlight(false);
      }
    },
    [subscribeToClient, generateResult]
  );

  const [generateInFlight, setGenerateInFlight] = useState(false);
  const [generateError, setGenerateError] = useState<Error | null>(null);

  const generateOtp = useCallback(async ({ phoneNumber }: FormDataType) => {
    setGenerateInFlight(true);
    try {
      const resp = await fetch("/auth/otp", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "X-Role-Claim": "subscriber",
        },
        body: JSON.stringify({
          phoneNumber: `+${generateResult.phoneNumber.replace(/\D/g, "")}`,
        }),
      });
      const data = await resp.json();
      if (!resp.ok) {
        throw setGenerateError(new Error(data.message));
      }
      setGenerateResult((s) => ({
        ...s,
        phoneNumber: `+${phoneNumber.replace(/\D/g, "")}`,
        code: data?.code, // for convenience when we're in dev mode
      }));
    } catch (e) {
      setGenerateError(e as Error);
    } finally {
      setGenerateInFlight(false);
    }
  }, []);

  if (!data.clientBySlug) {
    return <div>Client not found</div>;
  }

  if (done) {
    return (
      <div>
        <p>
          You're successfully subscribed to{" "}
          <strong>{data.clientBySlug.name}</strong>!
        </p>
      </div>
    );
  }

  return (
    <div>
      <h1>Subscribe to {data.clientBySlug?.name}</h1>
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
      {subscribeToClientError && (
        <ErrorDialog
          title="Failed to create account"
          error={subscribeToClientError} // TODO: show all errors
          startOpen={!!subscribeToClientError}
          onClose={() => setCreateClientError(null)}
        />
      )}
    </div>
  );
}
