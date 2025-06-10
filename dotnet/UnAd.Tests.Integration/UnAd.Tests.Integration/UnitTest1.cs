using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace UnAd.Tests.Integration;

public class PlanIntegrationTests : IClassFixture<GraphQLGatewayFixture> {
    private readonly HttpClient _httpClient;

    public PlanIntegrationTests(GraphQLGatewayFixture fixture) {
        // HttpClient is configured with the GraphQLGateway endpoint
        _httpClient = fixture.Client;
    }

    [Fact]
    public async Task CreatePlan_HappyPath_ShouldReturnPlanId() {
        // Arrange
        var mutation = @"
                mutation PlanEditorCreatePlanMutation($createPlanInput: CreatePlanInput!) {
                    createPlan(input: $createPlanInput) {
                        plan {
                            id
                        }
                    }
                }";

        // Define sample input data for the mutation.
        var variables = new {
            createPlanInput = new {
                name = "Test Plan",
                description = "This is a test plan",
                iconUrl = "http://example.com/icon.png",
                activateOnCreate = true,
                features = new {
                    maxMessages = 100,
                    supportTier = "BASIC",
                    aiSupport = false
                },
                priceTiers = new[]
                {
                    new {
                        name = "Standard Tier",
                        price = 9.99,
                        duration = "P30D",
                        description = "Monthly pricing tier",
                        iconUrl = "http://example.com/tier-icon.png"
                    }
                }
            }
        };

        var payload = new {
            query = mutation,
            variables = variables
        };

        // Serialize the payload to JSON.
        var jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Act
        var response = await _httpClient.PostAsync("/graphql", content);

        // For now, we'll leave the response handling for the next steps of the test.
        // Later on you can assert on the response status code, parse the JSON, and query the DB to verify side-effects.
        response.EnsureSuccessStatusCode();
    }
}

// A simple fixture to set up an HttpClient for your GraphQL Gateway.
public class GraphQLGatewayFixture : IDisposable {
    public HttpClient Client { get; }

    public GraphQLGatewayFixture() {
        // Adjust the base address if needed to match your docker-compose setup.
        Client = new HttpClient {
            BaseAddress = new Uri("http://localhost:5900")
        };
    }

    public void Dispose() {
        Client.Dispose();
    }
}
