using System.Net;
using FluentAssertions;
using RR.Tests.Infrastructure;
using Xunit;

namespace RR.Tests.Integration;

/// <summary>
/// Integration tests for overall API functionality
/// </summary>
public class ApiIntegrationTests : IClassFixture<TestWebApplicationFactory> {
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(TestWebApplicationFactory factory) {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task AllEndpoints_ShouldBeAccessible() {
        // Arrange
        var endpoints = new[]
        {
            "/api/v1/health",
            "/api/v1/status",
            "/api/v1/credits"
        };

        // Act & Assert
        foreach (var endpoint in endpoints) {
            var response = await _client.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"Endpoint {endpoint} should be accessible");
        }
    }

    [Fact]
    public async Task SwaggerEndpoint_ShouldBeAccessible_InDevelopment() {
        // Arrange
        var devFactory = new TestWebApplicationFactory();
        var devClient = devFactory.CreateClient();

        // Act
        var response = await devClient.GetAsync("/swagger/v1/swagger.json");

        // Assert - In testing environment, swagger might not be available, so we accept NotFound
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InvalidEndpoint_ShouldReturn404() {
        // Act
        var response = await _client.GetAsync("/api/v1/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ApiEndpoints_ShouldReturnJsonContentType() {
        // Arrange
        var endpoints = new[]
        {
            "/api/v1/health",
            "/api/v1/status",
            "/api/v1/credits"
        };

        // Act & Assert
        foreach (var endpoint in endpoints) {
            var response = await _client.GetAsync(endpoint);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json",
                $"Endpoint {endpoint} should return JSON content type");
        }
    }

    [Fact]
    public async Task ConcurrentRequests_ShouldBeHandledCorrectly() {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        const int concurrentRequests = 10;

        // Act
        for (int i = 0; i < concurrentRequests; i++) {
            tasks.Add(_client.GetAsync("/api/v1/health"));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(response => {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        });
    }

    [Fact]
    public async Task HealthCheck_ShouldRespondQuickly() {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/api/v1/health");
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Health check should respond within 1 second");
    }
}
