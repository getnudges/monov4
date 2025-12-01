
# Nudges System POC Repository

Welcome! This repository contains all components for the Nudges system proof-of-concept (POC), including backend, frontend, database, and infrastructure code.

---

## Quick Start

### 1. Prerequisites

You will need:
- A [Stripe](https://stripe.com/) account (free)
- An [ngrok](https://ngrok.com/) account (free)
- [.NET SDK](https://dotnet.microsoft.com/download) (for dev-certs)
- PowerShell (for running scripts)

---

### 2. Setup ngrok

1. Create an ngrok account and follow their [setup instructions](https://ngrok.com/docs/getting-started).
2. In the `ngrok/` directory, create a file (e.g., `ngrok.yml`) with the following contents:

  ```yaml
  version: "2"
  authtoken: <your_auth_token_here>

  tunnels:
    webhooks:
    proto: http
    addr: host.docker.internal:7071
    domain: <your_domain_here>.ngrok-free.dev
  ```
  - Replace `<your_auth_token_here>` and `<your_domain_here>` with your ngrok credentials.

---

### 3. Setup Stripe

1. Create a Stripe account.
2. In the Stripe dashboard, go to **Developers > Webhooks** and add a new endpoint:
  - URL: `https://<your_domain_here>.ngrok-free.dev/api/StripeWebhookHandler?code=<your_choice>`
  - Events to send: `product.created`
  - Copy the **Signing secret** (you'll need it below).

---

### 4. Environment Configuration

At the root of the repo, create a file named `.env.external` with the following:

```ini
STRIPE_API_KEY=<your_stripe_api_key>
STRIPE_WEBHOOKS_SECRET=<your_stripe_webhook_secret>
WEBHOOKS_API_KEY=<your_choice>  # must match the `code` in the webhook URL

# (Optional, not needed for demo)
TWILIO_ACCOUNT_SID=xxx
TWILIO_AUTH_TOKEN=xxx
TWILIO_MESSAGE_SERVICE_SID=xxx
```

**Notes:**
- `STRIPE_API_KEY`: Found in your Stripe dashboard.
- `STRIPE_WEBHOOKS_SECRET`: Generated when you create the webhook endpoint.
- `WEBHOOKS_API_KEY`: Any value you choose, but it must match the `code` query parameter in the webhook URL above.

---

### 5. Start the System

After cloning the repo and completing the steps above:

1. Open a terminal and navigate to the repo root.
2. Run:
  1. `dotnet dev-certs https -ep ./certs/aspnetapp.pfx` ([details](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-dev-certs))
  2. `./certs/generate-certs.ps1`
  3. `./start-dev.ps1`
3. Wait for all services to build and start (can take up to 20 minutes on first run).
4. Open `https://localhost:5050` in your browser.
5. Log in with:
  - **Username:** `+15555555555`
  - **Password:** `pass`

If you see a "View Plans" button, the system is running!

---

## Creating Your First Plan

1. Log in as above.
2. Click **View Plans** > **Create Plan**.
3. Enter a **Name** and **Max Messages** (minimum required fields).
4. Save changes.

If the "Foreign Service ID" field updates, everything is working.

---

## How It Works (Behind the Scenes)

1. User clicks "Save Changes" in the React UI
2. GraphQL mutation sent to GraphQLGateway → ProductApi
3. ProductApi saves to DB, emits PlanCreatedEvent
4. plans-listener consumes event, calls Stripe API to create product
5. Stripe fires `product.created` webhook (via ngrok)
6. Webhooks service emits ForeignProductSynchronizedEvent
7. foreign-product-listener consumes event, updates DB with Stripe product ID
8. GraphQL subscription updates React UI with Foreign Service ID

---

## Troubleshooting

- If something doesn't work, check the terminal output for errors.
- Ensure your `.env.external` and ngrok config are correct.
- Make sure all prerequisites are installed.

---

## Questions?

Open an issue or ask in the project discussions!

