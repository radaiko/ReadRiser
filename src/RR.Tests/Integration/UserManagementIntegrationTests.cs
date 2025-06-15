using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RR.DTO;
using RR.Tests.Infrastructure;

namespace RR.Tests.Integration;

[TestClass]
public class UserManagementIntegrationTests {
    private TestWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [TestInitialize]
    public void Setup() {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [TestCleanup]
    public void Cleanup() {
        _client?.Dispose();
        _factory?.Dispose();
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
        Assert.IsNull(createdUser.ParentId);
    }

    [TestMethod]
    public async Task CreateUser_ParentCreatesKid_ShouldSucceed() {
        // Arrange - Parent creates a kid
        _client.DefaultRequestHeaders.Add("X-User-ID", "parent-001");

        var createRequest = new CreateUserRequest(
            Username: "testkid",
            DisplayName: "Test Kid",
            Role: UserRole.Kid,
            ParentId: "parent-001"
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
        Assert.AreEqual("testkid", createdUser.Username);
        Assert.AreEqual(UserRole.Kid, createdUser.Role);
        Assert.AreEqual("parent-001", createdUser.ParentId);
    }

    [TestMethod]
    public async Task CreateUser_KidTriesToCreateUser_ShouldFail() {
        // Arrange - Kid tries to create another user
        _client.DefaultRequestHeaders.Add("X-User-ID", "kid-001");

        var createRequest = new CreateUserRequest(
            Username: "anotherkid",
            DisplayName: "Another Kid",
            Role: UserRole.Kid,
            ParentId: "parent-001"
        );

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/v1/users",
            createRequest,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        // Assert
        Assert.AreEqual(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task CreateUser_ParentTriesToCreateAdmin_ShouldFail() {
        // Arrange - Parent tries to create admin
        _client.DefaultRequestHeaders.Add("X-User-ID", "parent-001");

        var createRequest = new CreateUserRequest(
            Username: "newadmin",
            DisplayName: "New Admin",
            Role: UserRole.Admin,
            ParentId: null
        );

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/v1/users",
            createRequest,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        // Assert
        Assert.AreEqual(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task GetUsers_AdminUser_ShouldSeeAllUsers() {
        // Arrange
        _client.DefaultRequestHeaders.Add("X-User-ID", "admin-001");

        // Act
        var response = await _client.GetAsync("/api/v1/users");

        // Assert
        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);

        var usersResponse = await response.Content.ReadFromJsonAsync<UsersListResponse>();
        Assert.IsNotNull(usersResponse);
        Assert.IsTrue(usersResponse.TotalCount >= 6); // At least the test data users
    }

    [TestMethod]
    public async Task GetUsers_ParentUser_ShouldSeeParentsAndOwnKids() {
        // Arrange
        _client.DefaultRequestHeaders.Add("X-User-ID", "parent-001");

        // Act
        var response = await _client.GetAsync("/api/v1/users");

        // Assert
        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);

        var usersResponse = await response.Content.ReadFromJsonAsync<UsersListResponse>();
        Assert.IsNotNull(usersResponse);

        // Should see admin, parents, and own kids
        var hasAdmin = usersResponse.Users.Any(u => u.Role == UserRole.Admin);
        var hasParents = usersResponse.Users.Any(u => u.Role == UserRole.Parent);
        var hasOwnKids = usersResponse.Users.Any(u => u.Role == UserRole.Kid && u.ParentId == "parent-001");
        var hasOtherKids = usersResponse.Users.Any(u => u.Role == UserRole.Kid && u.ParentId != "parent-001");

        Assert.IsTrue(hasAdmin);
        Assert.IsTrue(hasParents);
        Assert.IsTrue(hasOwnKids);
        Assert.IsFalse(hasOtherKids); // Should not see other parents' kids
    }

    [TestMethod]
    public async Task GetUsers_KidUser_ShouldSeeParentAndOtherKids() {
        // Arrange
        _client.DefaultRequestHeaders.Add("X-User-ID", "kid-001");

        // Act
        var response = await _client.GetAsync("/api/v1/users");

        // Assert
        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);

        var usersResponse = await response.Content.ReadFromJsonAsync<UsersListResponse>();
        Assert.IsNotNull(usersResponse);

        // Should see their parent and all kids
        var hasOwnParent = usersResponse.Users.Any(u => u.Id == "parent-001");
        var hasKids = usersResponse.Users.Any(u => u.Role == UserRole.Kid);
        var hasOtherParents = usersResponse.Users.Any(u => u.Role == UserRole.Parent && u.Id != "parent-001");

        Assert.IsTrue(hasOwnParent);
        Assert.IsTrue(hasKids);
        Assert.IsFalse(hasOtherParents); // Should not see other parents
    }

    [TestMethod]
    public async Task GetUserById_ValidAccess_ShouldReturnUser() {
        // Arrange
        _client.DefaultRequestHeaders.Add("X-User-ID", "admin-001");

        // Act
        var response = await _client.GetAsync("/api/v1/users/parent-001");

        // Assert
        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.IsNotNull(user);
        Assert.AreEqual("parent-001", user.Id);
        Assert.AreEqual("parent1", user.Username);
    }

    [TestMethod]
    public async Task GetUserById_InvalidAccess_ShouldReturnForbidden() {
        // Arrange - Kid tries to access different parent
        _client.DefaultRequestHeaders.Add("X-User-ID", "kid-001");

        // Act
        var response = await _client.GetAsync("/api/v1/users/parent-002");

        // Assert
        Assert.AreEqual(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }
}
