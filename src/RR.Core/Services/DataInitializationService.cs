using Microsoft.Extensions.Logging;
using RR.Core.Interfaces;
using RR.Core.Models;
using RR.DTO;

namespace RR.Core.Services;

/// <summary>
/// Service for initializing test data in the file-based database
/// </summary>
public class DataInitializationService {
    private readonly IFileBasedDbService _dbService;
    private readonly ILogger<DataInitializationService> _logger;

    public DataInitializationService(IFileBasedDbService dbService, ILogger<DataInitializationService> logger) {
        _dbService = dbService;
        _logger = logger;
    }

    public void InitializeTestData() {
        var users = _dbService.GetUsers();
        if (users.Any()) {
            _logger.LogInformation("Test data already exists, skipping initialization");
            return;
        }

        _logger.LogInformation("Initializing test data...");

        // Create admin user
        var admin = new User {
            Id = "admin-001",
            Username = "admin",
            DisplayName = "System Administrator",
            Role = UserRole.Admin,
            ParentId = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "system"
        };
        _dbService.SaveUser(admin);

        // Create parent users
        var parent1 = new User {
            Id = "parent-001",
            Username = "parent1",
            DisplayName = "John Parent",
            Role = UserRole.Parent,
            ParentId = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = admin.Id
        };
        _dbService.SaveUser(parent1);

        var parent2 = new User {
            Id = "parent-002",
            Username = "parent2",
            DisplayName = "Jane Parent",
            Role = UserRole.Parent,
            ParentId = null,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = admin.Id
        };
        _dbService.SaveUser(parent2);

        // Create kid users
        var kid1 = new User {
            Id = "kid-001",
            Username = "kid1",
            DisplayName = "Alice Kid",
            Role = UserRole.Kid,
            ParentId = parent1.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = parent1.Id
        };
        _dbService.SaveUser(kid1);

        var kid2 = new User {
            Id = "kid-002",
            Username = "kid2",
            DisplayName = "Bob Kid",
            Role = UserRole.Kid,
            ParentId = parent1.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = parent1.Id
        };
        _dbService.SaveUser(kid2);

        var kid3 = new User {
            Id = "kid-003",
            Username = "kid3",
            DisplayName = "Charlie Kid",
            Role = UserRole.Kid,
            ParentId = parent2.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = parent2.Id
        };
        _dbService.SaveUser(kid3);

        // Update parent's children lists
        parent1.ChildrenIds.AddRange(new[] { kid1.Id, kid2.Id });
        parent2.ChildrenIds.Add(kid3.Id);
        _dbService.SaveUser(parent1);
        _dbService.SaveUser(parent2);

        _logger.LogInformation("Test data initialization completed. Created {UserCount} users", 6);
        _logger.LogInformation("Test user IDs:");
        _logger.LogInformation("  Admin: {AdminId}", admin.Id);
        _logger.LogInformation("  Parent1: {Parent1Id}", parent1.Id);
        _logger.LogInformation("  Parent2: {Parent2Id}", parent2.Id);
        _logger.LogInformation("  Kid1: {Kid1Id}", kid1.Id);
        _logger.LogInformation("  Kid2: {Kid2Id}", kid2.Id);
        _logger.LogInformation("  Kid3: {Kid3Id}", kid3.Id);
    }
}
