using System.Net;
using FluentAssertions;
using RR.DTO;
using RR.Tests.Infrastructure;
using Xunit;

namespace RR.Tests.Endpoints;

/// <summary>
/// Integration tests for the Health endpoint
/// </summary>
public class HealthEndpointTests : IClassFixture<TestWebApplicationFactory> {
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public HealthEndpointTests(TestWebApplicationFactory factory) {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ShouldReturnOk_WithValidHealthResponse() {
        // Act
        var response = await _client.GetAsync("/api/v1/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadAsStringAsync();
        var healthResponse = TestJsonHelper.Deserialize<HealthResponse>(content);

        healthResponse.Should().NotBeNull();
        healthResponse!.Status.Should().Be("Healthy");
        healthResponse.Environment.Should().Be("Testing");
        healthResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetHealth_ShouldReturnConsistentResponse_WhenCalledMultipleTimes() {
        // Act
        var response1 = await _client.GetAsync("/api/v1/health");
        var response2 = await _client.GetAsync("/api/v1/health");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();

        var healthResponse1 = TestJsonHelper.Deserialize<HealthResponse>(content1);

        var healthResponse2 = TestJsonHelper.Deserialize<HealthResponse>(content2);

        healthResponse1!.Status.Should().Be(healthResponse2!.Status);
        healthResponse1.Environment.Should().Be(healthResponse2.Environment);
    }

    [Fact]
    public async Task GetHealth_ShouldHaveCorrectContentType() {
        // Act
        var response = await _client.GetAsync("/api/v1/health");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        response.Content.Headers.ContentType?.CharSet.Should().BeOneOf("utf-8", null);
    }

    [Fact]
    public async Task GetHealth_ShouldIncludeRequiredFields() {
        // Act
        var response = await _client.GetAsync("/api/v1/health");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("status");
        content.Should().Contain("timestamp");
        content.Should().Contain("environment");
    }
}
