import { type RelayRoute } from "../../Router/withRelay";
import type { LoginQuery } from "./__generated__/LoginQuery.graphql";

import { Redirect } from "wouter";
import { useEffect } from "react";

const HomePath = "/";

export default function LoginPage({ data }: Readonly<RelayRoute<LoginQuery>>) {
  useEffect(() => {
    if (data.viewer?.id) {
      return;
    }
    window.location.href = `/auth/login?redirectUri=${encodeURIComponent(
      window.location.href
    )}`;
  }, [data.viewer?.id]);

  if (data.viewer?.id) {
    return <Redirect to={HomePath} />;
  }

  return null;
}
