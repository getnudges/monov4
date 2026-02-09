import { expect, test, type Page } from "@playwright/test";

type ViewerState =
  | { kind: "none" }
  | { kind: "client"; subscriptionId: string | null };

type GraphqlState = {
  signUpQueryCount: number;
  createClientCalled: boolean;
  lastCreateClientName?: string;
};

const seedSignupQuery = (viewer: ViewerState) => {
  if (viewer.kind === "none") {
    return {
      data: {
        viewer: null,
        totalClients: 123,
      },
    };
  }

  return {
    data: {
      viewer: {
        __typename: "Client",
        id: "client-1",
        subscriptionId: viewer.subscriptionId,
        subscription: viewer.subscriptionId
          ? { status: "ACTIVE", id: viewer.subscriptionId }
          : null,
      },
      totalClients: 123,
    },
  };
};

const setupGraphqlMock = (
  page: Page,
  viewerAfterVerify: ViewerState,
  state: GraphqlState
) => {
  return page.route("**/graphql", async (route) => {
    const request = route.request();
    const payload = request.postDataJSON() as {
      operationName?: string;
      variables?: Record<string, unknown>;
      query?: string;
    };

    if (payload?.operationName === "SignUpQuery") {
      state.signUpQueryCount += 1;
      const response =
        state.signUpQueryCount > 1
          ? seedSignupQuery(viewerAfterVerify)
          : seedSignupQuery({ kind: "none" });

      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(response),
      });
    }

    if (payload?.operationName === "SignUp_CreateClientMutation") {
      state.createClientCalled = true;
      const name = (payload.variables?.createClientInput as { name?: string })
        ?.name;
      state.lastCreateClientName = name;

      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          data: {
            createClient: {
              client: { id: "client-new" },
              errors: [],
            },
          },
        }),
      });
    }

    return route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ data: {} }),
    });
  });
};

const setupAuthMocks = (page: Page) => {
  return Promise.all([
    page.route("**/auth/otp", async (route) => {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ code: "123456" }),
      });
    }),
    page.route("**/auth/otp/verify", async (route) => {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({ ok: true }),
      });
    }),
  ]);
};

const fillGenerateForm = async (page: Page) => {
  await page.getByLabel("Your Business Name").fill("Acme Co");
  await page.getByLabel("Phone Number").fill("4155551212");
  await page.getByRole("button", { name: "Get OTP" }).click();
};

const fillOtp = async (page: Page) => {
  await page.getByLabel("One-Time Password").fill("123456");
  await page.getByRole("button", { name: "Verify" }).click();
};

test("signup redirects to dashboard when subscribed", async ({ page }) => {
  const state: GraphqlState = {
    signUpQueryCount: 0,
    createClientCalled: false,
  };

  await setupGraphqlMock(
    page,
    { kind: "client", subscriptionId: "sub-1" },
    state
  );
  await setupAuthMocks(page);

  await page.goto("/signup");
  await fillGenerateForm(page);
  await fillOtp(page);

  await expect(page).toHaveURL(/\/dashboard$/);
});

test("signup redirects to plans when not subscribed", async ({ page }) => {
  const state: GraphqlState = {
    signUpQueryCount: 0,
    createClientCalled: false,
  };

  await setupGraphqlMock(page, { kind: "client", subscriptionId: null }, state);
  await setupAuthMocks(page);

  await page.goto("/signup");
  await fillGenerateForm(page);
  await fillOtp(page);

  await expect(page).toHaveURL(/\/plans$/);
});

test("signup creates client and redirects to plans", async ({ page }) => {
  const state: GraphqlState = {
    signUpQueryCount: 0,
    createClientCalled: false,
  };

  await setupGraphqlMock(page, { kind: "none" }, state);
  await setupAuthMocks(page);

  await page.goto("/signup");
  await fillGenerateForm(page);
  await fillOtp(page);

  await expect(page).toHaveURL(/\/plans$/);
  expect(state.createClientCalled).toBe(true);
  expect(state.lastCreateClientName).toBe("Acme Co");
});
