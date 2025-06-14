using System.Net;
using FluentAssertions;
using RR.DTO;
using RR.Tests.Infrastructure;
using Xunit;

namespace RR.Tests.Endpoints;

/// <summary>
/// Integration tests for the Credits endpoint
/// </summary>
public class CreditsEndpointTests : IClassFixture<TestWebApplicationFactory> {
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CreditsEndpointTests(TestWebApplicationFactory factory) {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetCredits_ShouldReturnOk_WithValidCreditsResponse() {
        // Act
        var response = await _client.GetAsync("/api/v1/credits");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadAsStringAsync();
        var creditsResponse = TestJsonHelper.Deserialize<CreditsResponse>(content);

        creditsResponse.Should().NotBeNull();
        creditsResponse!.ApplicationName.Should().Be("ReadRiser API");
        creditsResponse.ApplicationVersion.Should().NotBeNullOrWhiteSpace();
        creditsResponse.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        creditsResponse.Packages.Should().NotBeNull();
        creditsResponse.TotalPackages.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCredits_ShouldReturnValidPackageInfo() {
        // Act
        var response = await _client.GetAsync("/api/v1/credits");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        var creditsResponse = TestJsonHelper.Deserialize<CreditsResponse>(content);

        creditsResponse!.Packages.Should().AllSatisfy(package => {
            package.Name.Should().NotBeNullOrWhiteSpace();
            package.Version.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public async Task GetCredits_ShouldHaveConsistentTotalPackagesCount() {
        // Act
        var response = await _client.GetAsync("/api/v1/credits");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        var creditsResponse = TestJsonHelper.Deserialize<CreditsResponse>(content);

        creditsResponse!.TotalPackages.Should().Be(creditsResponse.Packages.Count);
    }

    [Fact]
    public async Task GetCredits_ShouldReturnValidVersion() {
        // Act
        var response = await _client.GetAsync("/api/v1/credits");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        var creditsResponse = TestJsonHelper.Deserialize<CreditsResponse>(content);

        creditsResponse!.ApplicationVersion.Should().MatchRegex(@"^\d+\.\d+\.\d+(\.\d+)?$");
    }

    [Fact]
    public async Task GetCredits_ShouldHaveCorrectContentType() {
        // Act
        var response = await _client.GetAsync("/api/v1/credits");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        response.Content.Headers.ContentType?.CharSet.Should().BeOneOf("utf-8", null);
    }

    [Fact]
    public async Task GetCredits_ShouldIncludeRequiredFields() {
        // Act
        var response = await _client.GetAsync("/api/v1/credits");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("applicationName");
        content.Should().Contain("applicationVersion");
        content.Should().Contain("generatedAt");
        content.Should().Contain("packages");
        content.Should().Contain("totalPackages");
    }

    [Fact]
    public async Task GetCredits_ShouldBeIdempotent() {
        // Act
        var response1 = await _client.GetAsync("/api/v1/credits");
        var response2 = await _client.GetAsync("/api/v1/credits");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();

        var creditsResponse1 = TestJsonHelper.Deserialize<CreditsResponse>(content1);

        var creditsResponse2 = TestJsonHelper.Deserialize<CreditsResponse>(content2);

        creditsResponse1!.ApplicationName.Should().Be(creditsResponse2!.ApplicationName);
        creditsResponse1.ApplicationVersion.Should().Be(creditsResponse2.ApplicationVersion);
        creditsResponse1.TotalPackages.Should().Be(creditsResponse2.TotalPackages);
    }
}
