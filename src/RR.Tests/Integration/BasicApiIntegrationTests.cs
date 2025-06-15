using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RR.DTO;
using RR.Tests.Infrastructure;

namespace RR.Tests.Integration;

[TestClass]
public class BasicApiIntegrationTests {
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
    public async Task GetHealth_ShouldReturnHealthy() {
        // Act
        var response = await _client.GetAsync("/api/v1/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var healthResponse = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.IsNotNull(healthResponse);
        Assert.AreEqual("Healthy", healthResponse.Status);
    }

    [TestMethod]
    public async Task GetStatus_ShouldReturnStatusWithEndpoints() {
        // Act
        var response = await _client.GetAsync("/api/v1/status");

        // Assert
        response.EnsureSuccessStatusCode();
        var statusResponse = await response.Content.ReadFromJsonAsync<StatusResponse>();
        Assert.IsNotNull(statusResponse);
        Assert.AreEqual("RR.Http", statusResponse.ApplicationName);
        Assert.IsTrue(statusResponse.AvailableEndpoints.Count > 0);
    }

    [TestMethod]
    public async Task GetCredits_ShouldReturnPackageInfo() {
        // Act
        var response = await _client.GetAsync("/api/v1/credits");

        // Assert
        response.EnsureSuccessStatusCode();
        var creditsResponse = await response.Content.ReadFromJsonAsync<CreditsResponse>();
        Assert.IsNotNull(creditsResponse);
        Assert.AreEqual("ReadRiser API", creditsResponse.ApplicationName);
        Assert.IsTrue(creditsResponse.TotalPackages >= 0);
    }

    [TestMethod]
    public async Task GetUsers_WithoutAuthHeader_ShouldReturnBadRequest() {
        // Act
        var response = await _client.GetAsync("/api/v1/users");

        // Assert
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateUser_AdminCreatesParent_ShouldSucceed() {
        // Arrange
        _client.DefaultRequestHeaders.Add("X-User-ID", "admin-001");

        var createRequest = new CreateUserRequest(
            Username: "testparent",
            DisplayName: "Test Parent",
            Role: UserRole.Parent,
            ParentId: null
        );

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/v1/users",
            createRequest,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        // Assert
        Assert.AreEqual(System.Net.HttpStatusCode.Created, response.StatusCode);

        var createdUser = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.IsNotNull(createdUser);
        Assert.AreEqual("testparent", createdUser.Username);
        Assert.AreEqual(UserRole.Parent, createdUser.Role);
    }
}
