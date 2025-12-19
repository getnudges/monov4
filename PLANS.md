# Feature Flags with Cohorts: Architecture Document for Nudges

## Overview

We're adding feature flags to Nudges using a rule-based cohort system. The initial implementation targets two primary use cases: role-based access (admin vs. user) and subscription-tier-based features (free vs. premium vs. enterprise). The architecture leverages your existing distributed GraphQL API, JWT-based authentication, and event-driven infrastructure to deliver feature flags with minimal performance overhead and clean separation of concerns.

## Core Architectural Decisions

### Cohort Definition Strategy

We're using rule-based cohort evaluation where cohorts are defined as collections of predicates that evaluate against user attributes. A cohort definition looks like a set of rules with operators and a combinator. For example, a premium admins cohort would specify that the role field must equal admin AND the subscription tier field must be in the set containing premium and enterprise.

This approach is extensible. The cohort definition format separates the evaluator type from the configuration, which means we can add percentage-based rollouts or explicit membership lists later without changing how flags reference cohorts. For now, we only need equals, in, not equals, and not in operators, which handle the role and tier use cases completely.

### When Cohorts Get Evaluated

The key architectural insight is that cohort evaluation happens at authentication time, not at request time. When a user authenticates with their one-time password, we evaluate all defined cohorts against that user's current attributes and include the resulting cohort memberships directly in the JWT as claims. This means feature flag checks become simple claim lookups with zero database hits and zero rule evaluation during request processing.

The trade-off is staleness. Users see the cohorts they belonged to when they authenticated, not necessarily their current cohorts if something changed since then. This is where refresh tokens come in.

## Authentication Flow with Claims Generation

### The Token Exchange Architecture

Your existing architecture already has the pieces we need. The client sends an opaque token to the gateway. The gateway looks up this token in Redis and retrieves a JWT which it forwards to the subgraphs. Each subgraph uses standard ASP.NET JWT middleware to deserialize and validate the claims.

What changes is what happens during initial authentication when we generate that JWT.

### Authentication and Token Generation Flow

When a user submits their one-time password, the Auth API validates it against Cognito in production or Keycloak locally. Once validated, the Auth API needs to generate the fat JWT that will contain all the claims the subgraphs need.

The Auth API makes a service-to-service call to the User API. This call goes through the gateway like everything else, using a service account token that identifies the Auth API itself. The request asks for user context for a specific user ID. The User API endpoint is internal-only and checks that the caller is the Auth API before responding.

The User API loads the user from the database, evaluates all defined cohort rules against that user's current attributes, and returns a claims object containing role, subscription tier, the list of cohorts the user belongs to, and any other business context needed for feature flags or authorization.

The Auth API combines these business claims with the claims from the OIDC provider. OIDC claims like subject and phone number are always fetched fresh on each token generation to avoid staleness issues with identity information. The business claims from the User API can be cached with change detection, which we'll discuss shortly.

The Auth API generates a single fat JWT containing all these claims, stores it in Redis keyed by an opaque token, and returns that opaque token to the client. Later requests from the client include this opaque token, the gateway looks it up in Redis, and forwards the fat JWT to whichever subgraph needs to handle the request.

### Claims Computation Layers

The User API's responsibility for generating claims is broken into three conceptual layers.

The first layer is pure claims computation. A ClaimsBuilder service takes a user object and returns a dictionary of claims. It evaluates all cohort rules against the user's attributes and produces a list of cohort names the user belongs to. This is a pure function with no side effects, which makes it easy to test. Given a user with a specific role and tier, we can assert they should belong to specific cohorts.

The second layer is change detection and caching. This wraps the ClaimsBuilder with an implementation of your existing cache interface. When claims are stored in the cache, this implementation compares the new claims to the previously cached claims using bit-for-bit comparison of the serialized form. If the claims changed, it triggers a notification before storing the new version.

The third layer is event publishing. When the cache detects that claims changed, a notifier publishes a message to Kafka indicating what changed. The message includes the user ID, the old claims, and the new claims. Other parts of the system can subscribe to these events and react accordingly.

This layering means the User API doesn't need to know about tokens or refresh mechanisms. It just knows how to compute claims, detect when they change, and notify interested parties.

## Feature Flag Evaluation in Subgraphs

Once the fat JWT reaches a subgraph, feature flag evaluation becomes trivial. The JWT claims already contain the list of cohorts the user belongs to. A feature flag service simply looks up the flag definition, checks which cohorts it targets, and sees if any of the user's cohorts are in that list.

Because you're using ASP.NET, you can make this even cleaner with authorization attributes. A RequireFeature attribute can be applied to resolvers or endpoints, and it uses the standard ASP.NET authorization pipeline to check claims. The subgraph never evaluates cohort rules, never hits a database, never calls another service. It just reads claims from the JWT that's already been validated by the middleware.

## Handling Claim Changes with Refresh Tokens

The staleness problem is real. If someone upgrades their subscription, you can't log them out and force them to re-authenticate just to see their new features. That's terrible user experience.

Refresh tokens solve this. The pattern is standard in OAuth but worth spelling out for your architecture.

An access token is your fat JWT stored in Redis with a short lifetime, maybe fifteen minutes to an hour. A refresh token is a separate long-lived token, lasting days or weeks, that proves the session is still valid.

When the access token expires, the client automatically sends the refresh token to a refresh endpoint in your Auth API. This endpoint validates the refresh token, extracts the user ID, and goes through the exact same flow as initial authentication. It calls the User API for fresh claims, combines them with fresh OIDC claims, generates a new fat JWT, stores it in Redis with a new opaque token, and returns that to the client. The user never sees this happen. It's automatic.

This means when someone's subscription changes, they don't immediately see new features, but they will within the next access token lifetime. For a fifteen minute access token TTL, the worst case staleness is fifteen minutes. For most changes like tier upgrades or role assignments, this is totally acceptable.

For critical changes that need immediate effect, like account suspension or fraud detection, you can still force invalidation. Maintain a force refresh flag in Redis keyed by user ID. Critical operations can check this flag before trusting the JWT. If it's set, return a 401 and let the client refresh immediately. Most operations skip this check.

## Event-Driven Refresh Triggers

Your existing event-driven architecture makes this pattern even better. When something changes that affects feature flags, publish an event. A SubscriptionChanged event fires when someone upgrades or downgrades. A RoleAssigned event fires when permissions change.

These events can trigger different responses based on urgency. For normal changes, the event does nothing to existing tokens. The user's claims will update naturally on their next token refresh. For urgent changes, the event sets the force refresh flag in Redis, causing the user's next request to trigger an immediate token refresh.

You can also use these events for other purposes. Log them for auditing. Update analytics. Send notifications. The claims change detection in the User API cache ensures these events only fire when something actually changed, not on every read.

## Service-to-Service Authentication

The Auth API needs to call the User API to get claims, and that call goes through the gateway. This creates a bootstrapping problem. How does the Auth API authenticate its request?

The answer is service account tokens. The Auth API has its own long-lived JWT that identifies it as the Auth API service. This token is cached and refreshed before expiry. When the Auth API needs to call the User API, it includes its service token in the request to the gateway.

The gateway validates the service token and routes the request to the User API. The User API checks that the caller is the Auth API before responding with claims data. This is the same pattern your other services already use for internal calls.

The important distinction is that the service token asserts the identity of the Auth API itself, while the JWT being generated asserts the identity and attributes of the end user. Two different tokens for two different purposes.

## OIDC Claims and Staleness

OIDC claims from Cognito or Keycloak present a boundary problem. These claims can change outside your control. A user might verify their email, change their phone number, or an admin might update something directly in the Keycloak console.

For your use case with OTP phone authentication, the OIDC claims are minimal. Mostly just the user ID and phone number. Phone numbers rarely change, and when they do it's basically a re-registration anyway.

The simplest approach is to always fetch OIDC claims fresh during token generation and refresh. Don't cache them. The OIDC call is probably fast, and it keeps your architecture simple. Your business claims get the full caching and change detection treatment, but OIDC claims are treated as always authoritative.

If you need to add new OIDC claims as part of a software update, the rollout is gradual and automatic. New tokens include the new claim. Old tokens in Redis lack the claim. Subgraphs handle missing claims gracefully with null-safe code. As tokens naturally refresh, everyone picks up the new claim.

## Implementation Checklist

To implement this architecture, you need a few components.

In the User API, implement the ClaimsBuilder service that evaluates cohort rules and produces claims. Implement the change-detecting cache wrapper using your existing cache interface. This wrapper does bit-for-bit comparison on set operations and triggers notifications on changes. Implement the claims change notifier that publishes to Kafka. Create an internal-only endpoint that accepts a user ID from the Auth API and returns the claims object.

In the Auth API, implement service account token caching and refresh. Implement the logic to call the User API for claims during token generation. Combine OIDC claims fetched fresh with cached business claims from the User API. Generate the fat JWT with all claims and store it in Redis. Implement the refresh token endpoint that re-validates and regenerates tokens.

In each subgraph, create a feature flag service that reads cohort claims from the JWT and checks them against flag definitions. Optionally create authorization attributes for declarative feature flag checks in your resolvers.

In your event consumers, subscribe to the claims changed events from Kafka and implement whatever reactions you need. For urgent changes, set force refresh flags. For analytics, log the changes. For auditing, store the history.

## Why This Architecture Works for Nudges

This design leverages infrastructure you already have. The distributed GraphQL API with gateway, the JWT-based authentication with Redis storage, the event-driven communication with Kafka, the service-to-service authentication pattern. Feature flags slot into these existing patterns without requiring new infrastructure.

The performance characteristics are good. Feature flag checks are just claim lookups with no I/O. Cohort evaluation happens once per authentication, not once per request. The only new I/O is the Auth API calling the User API during token generation, and that's one call per authentication, not per request.

The separation of concerns is clean. The User API owns user data and cohort evaluation. The Auth API owns token generation and lifecycle. The subgraphs own feature flag checks and business logic. Each piece has a clear responsibility and well-defined interfaces to the others.

The extensibility is built in. Adding percentage-based rollouts or explicit membership lists just means adding new cohort evaluator types. Adding new claim sources just means calling another internal API during token generation. Adding new event consumers just means subscribing to the Kafka topic.

The operational characteristics match your use case. For small businesses using the system intermittently, longer access token lifetimes are fine. The refresh mechanism provides eventual consistency for changes while avoiding the terrible experience of forced logouts. For the few cases that need immediate propagation, you have the force refresh mechanism.
