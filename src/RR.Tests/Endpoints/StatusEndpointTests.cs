using System.Net;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RR.DTO;
using RR.Tests.Infrastructure;

namespace RR.Tests.Endpoints;

/// <summary>
/// Integration tests for the Status endpoint
/// </summary>
[TestClass]
public class StatusEndpointTests {
    private static TestWebApplicationFactory _factory = null!;
    private static HttpClient _client = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context) {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [ClassCleanup]
    public static void ClassCleanup() {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [TestMethod]
    public async Task GetStatus_ShouldReturnOk_WithValidStatusResponse() {
        // Act
        var response = await _client.GetAsync("/api/v1/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadAsStringAsync();
        var statusResponse = TestJsonHelper.Deserialize<StatusResponse>(content);

        statusResponse.Should().NotBeNull();
        statusResponse!.ApplicationName.Should().Be("RR.Http");
        statusResponse.Version.Should().NotBeNullOrWhiteSpace();
        statusResponse.Environment.Should().Be("Testing");
        statusResponse.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        statusResponse.AvailableEndpoints.Should().NotBeEmpty();
    }

    [TestMethod]
    public async Task GetStatus_ShouldIncludeExpectedEndpoints() {
        // Act
        var response = await _client.GetAsync("/api/v1/status");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        var statusResponse = TestJsonHelper.Deserialize<StatusResponse>(content);

        statusResponse!.AvailableEndpoints.Should().Contain(e => e.Path.Contains("/api/v1/health"));
        statusResponse.AvailableEndpoints.Should().Contain(e => e.Path.Contains("/api/v1/status"));
        statusResponse.AvailableEndpoints.Should().Contain(e => e.Path.Contains("/api/v1/credits"));
    }

    [TestMethod]
    public async Task GetStatus_ShouldHaveValidEndpointStructure() {
        // Act
        var response = await _client.GetAsync("/api/v1/status");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        var statusResponse = TestJsonHelper.Deserialize<StatusResponse>(content);

        statusResponse!.AvailableEndpoints.Should().AllSatisfy(endpoint => {
            endpoint.Path.Should().NotBeNullOrWhiteSpace();
            endpoint.Method.Should().NotBeNullOrWhiteSpace();
            endpoint.Method.Should().BeOneOf("GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS");
        });
    }

    [TestMethod]
    public async Task GetStatus_ShouldReturnValidVersion() {
        // Act
        var response = await _client.GetAsync("/api/v1/status");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        var statusResponse = TestJsonHelper.Deserialize<StatusResponse>(content);

        statusResponse!.Version.Should().MatchRegex(@"^\d+\.\d+\.\d+(\.\d+)?$");
    }

    [TestMethod]
    public async Task GetStatus_ShouldHaveCorrectContentType() {
        // Act
        var response = await _client.GetAsync("/api/v1/status");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        response.Content.Headers.ContentType?.CharSet.Should().BeOneOf("utf-8", null);
    }

    [TestMethod]
    public async Task GetStatus_ShouldIncludeRequiredFields() {
        // Act
        var response = await _client.GetAsync("/api/v1/status");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("applicationName");
        content.Should().Contain("version");
        content.Should().Contain("timestamp");
        content.Should().Contain("environment");
        content.Should().Contain("availableEndpoints");
    }
}
