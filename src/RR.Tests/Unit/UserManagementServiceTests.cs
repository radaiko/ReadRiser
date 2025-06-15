using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RR.Core.Interfaces;
using RR.Core.Models;
using RR.Core.Services;
using RR.DTO;

namespace RR.Tests.Unit;

[TestClass]
public class UserManagementServiceTests {
    private IFileBasedDbService _dbService = null!;
    private IUserManagementService _userService = null!;
    private string _tempDir = null!;

    [TestInitialize]
    public void TestInitialize() {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> {
                ["FileStorage:DatabasePath"] = _tempDir
            })
            .Build();

        _dbService = new FileBasedDbService(configuration);
        _userService = new UserManagementService(_dbService);

        // Create test admin user
        var admin = new User {
            Id = "admin-test",
            Username = "admin",
            DisplayName = "Test Admin",
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };
        _dbService.SaveUser(admin);
    }

    [TestCleanup]
    public void TestCleanup() {
        if (Directory.Exists(_tempDir)) {
            Directory.Delete(_tempDir, true);
        }
    }

    [TestMethod]
    public void CreateUser_AdminCreatesParent_ShouldSucceed() {
        // Arrange
        var request = new CreateUserRequest(
            Username: "parent1",
            DisplayName: "Parent User",
            Role: DTO.UserRole.Parent,
            ParentId: null
        );

        // Act
        var result = _userService.CreateUser(request, "admin-test");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("parent1", result.Username);
        Assert.AreEqual(DTO.UserRole.Parent, result.Role);
        Assert.IsNull(result.ParentId);
        Assert.AreEqual("admin-test", result.CreatedBy);
    }

    [TestMethod]
    public void CreateUser_ParentTriesToCreateAdmin_ShouldThrow() {
        // Arrange
        var parent = new User {
            Id = "parent-test",
            Username = "parent",
            DisplayName = "Test Parent",
            Role = UserRole.Parent,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "admin-test"
        };
        _dbService.SaveUser(parent);

        var request = new CreateUserRequest(
            Username: "admin2",
            DisplayName: "Another Admin",
            Role: DTO.UserRole.Admin,
            ParentId: null
        );

        // Act & Assert
        Assert.ThrowsException<UnauthorizedAccessException>(() =>
            _userService.CreateUser(request, "parent-test"));
    }

    [TestMethod]
    public void CreateUser_KidTriesToCreateUser_ShouldThrow() {
        // Arrange
        var parent = new User {
            Id = "parent-test",
            Username = "parent",
            DisplayName = "Test Parent",
            Role = UserRole.Parent,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "admin-test"
        };
        _dbService.SaveUser(parent);

        var kid = new User {
            Id = "kid-test",
            Username = "kid",
            DisplayName = "Test Kid",
            Role = UserRole.Kid,
            ParentId = "parent-test",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "parent-test"
        };
        _dbService.SaveUser(kid);

        var request = new CreateUserRequest(
            Username: "kid2",
            DisplayName: "Another Kid",
            Role: DTO.UserRole.Kid,
            ParentId: "parent-test"
        );

        // Act & Assert
        Assert.ThrowsException<UnauthorizedAccessException>(() =>
            _userService.CreateUser(request, "kid-test"));
    }

    [TestMethod]
    public void GetUsersForUser_AdminUser_ShouldSeeAllUsers() {
        // Arrange - Create various users
        var parent = new User {
            Id = "parent-test",
            Username = "parent",
            DisplayName = "Test Parent",
            Role = UserRole.Parent,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "admin-test"
        };
        _dbService.SaveUser(parent);

        var kid = new User {
            Id = "kid-test",
            Username = "kid",
            DisplayName = "Test Kid",
            Role = UserRole.Kid,
            ParentId = "parent-test",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "parent-test"
        };
        _dbService.SaveUser(kid);

        // Act
        var result = _userService.GetUsersForUser("admin-test");

        // Assert
        Assert.IsTrue(result.Count >= 3); // Admin, Parent, Kid
        Assert.IsTrue(result.Any(u => u.Role == DTO.UserRole.Admin));
        Assert.IsTrue(result.Any(u => u.Role == DTO.UserRole.Parent));
        Assert.IsTrue(result.Any(u => u.Role == DTO.UserRole.Kid));
    }
}
