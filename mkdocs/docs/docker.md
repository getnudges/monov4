# Considerations for Docker

Each component in this system that is designed to be run in a container has an associated `Dockerfile` within it's project directory (or in a nearby directory, such as with the .NET code).  This file is used to construct the images that will be deployed to their respective containers in ECS.

## Conventions

Each deployable component of the system should have a Dockerfile, and depending on it's build dependency requirements, it may or may not live in the same directory as the component itself.  In general, if a component has dependencies on other parts of the system, that Dockerfile should live at the highest possible place in the directory structure without (ideally), making it all the way to the root.  Also, if the Dockerfile does not live alongside it's component, it should be named after the component.  For example, many of the .NET projects rely on other .NET projects, so many of those Dockerfiles live in the `dotnet` directly.  `DbSeeder.Dockerfile`, for instance, lives in `dotnet` because it depends on `Nudges.Redis`, `Nudges.Data` and others, and referencing those projects with relative paths from that directory is far less messy than the mass of `../../../.......` that would be necessary otherwise.

## Local Development

I've tried hard to make development of the entire system locally as easy as possible.  For convenience, there are a few scripts at the root of the project to get things running locally.

### Prerequisites

Before running anything locally, there is at least one file needed:  `dotnet/.env.docker`.  The base template looks like this:

??? note "Example `.env.docker`"
    ```ini
    REDIS_URL="redis:6379"

    ConnectionStrings__UserDb="Host=postgres;Port=5432;Database=userdb;Username=userdb;Password=userdb;"
    ConnectionStrings__ProductDb="Host=postgres;Port=5432;Database=productdb;Username=productdb;Password=productdb;"
    ConnectionStrings__PaymentDb="Host=postgres;Port=5432;Database=paymentdb;Username=paymentdb;Password=paymentdb;"

    API_KEY="test"

    GRAPH_MONITOR_URL="http://graph-monitor:5145"

    STRIPE_API_KEY="sk_test_51MtKShE8A2efFCQSK8cxjP720Ya7fl0JFpvrPc1pUR1dqiOEhuOjC07cn9YLNBxPH38a1vZLMkGGhuApBQr90E3J00aqS8IsGu"

    TWILIO_ACCOUNT_SID="AC61b60bdd31061ba49d77f3dfaa2f925e"
    TWILIO_AUTH_TOKEN="b1086b84b834deb3741387906e8d2eb8"
    TWILIO_MESSAGE_SERVICE_SID="MGcf6c611882037368d846157e252d7dc5"

    Kafka__BrokerList="kafka:29092"
    GRAPHQL_API_URL="http://graphql-gateway:5900/graphql"

    COGNITO_CLIENT_ID="7o8na736debi5u11kutaoao4qr"
    COGNITO_CLIENT_SECRET=""
    COGNITO_POOL_DOMAIN="nudges-development"
    COGNITO_POOL_DOMAIN_ID="nudges-development"
    COGNITO_POOL_ENDPOINT="cognito-idp.us-east-2.amazonaws.com/us-east-2_xD41Z0dWM"
    COGNITO_USER_POOL_ID="us-east-2_xD41Z0dWM"

    ```

Not all projects need all of these values, but for convenience, they all live in one place.

### `start-dev`

The `start-dev.ps1` script is the basic "get me going" script for most development.  It initiates all of the services and UIs in Docker.


