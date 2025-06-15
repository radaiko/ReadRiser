using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace RR.DTO;

/// <summary>
/// User roles in the system with hierarchical permissions
/// </summary>
public enum UserRole {
    /// <summary>
    /// Administrator with highest privileges - can create Parent users only
    /// </summary>
    Admin = 1,

    /// <summary>
    /// Parent user - can create Kid users and share files with their Kids and other Parents
    /// </summary>
    Parent = 2,

    /// <summary>
    /// Kid user - can share files with other Kids, access files from their Parent or other Kids
    /// </summary>
    Kid = 3
}

/// <summary>
/// Request model for creating a new user
/// </summary>
/// <param name="Username">Unique username for the user</param>
/// <param name="DisplayName">Display name for the user</param>
/// <param name="Role">Role to assign to the user</param>
/// <param name="ParentId">Required for Kid users - ID of their parent</param>
public record CreateUserRequest(
    [Description("Unique username for the new user")]
    [Required]
    [StringLength(50, MinimumLength = 3)]
    string Username,

    [Description("Display name for the user")]
    [Required]
    [StringLength(100, MinimumLength = 1)]
    string DisplayName,

    [Description("Role to assign to the user")]
    [Required]
    UserRole Role,

    [Description("Parent user ID - required when creating Kid users")]
    string? ParentId
);

/// <summary>
/// Response model for user operations
/// </summary>
/// <param name="Id">Unique identifier for the user</param>
/// <param name="Username">Username of the user</param>
/// <param name="DisplayName">Display name of the user</param>
/// <param name="Role">User's role in the system</param>
/// <param name="ParentId">Parent user ID for Kid users</param>
/// <param name="CreatedAt">When the user was created</param>
/// <param name="CreatedBy">Who created this user</param>
public record UserResponse(
    [Description("Unique identifier for the user")]
    [Required]
    string Id,

    [Description("Username of the user")]
    [Required]
    string Username,

    [Description("Display name of the user")]
    [Required]
    string DisplayName,

    [Description("User's role in the system")]
    [Required]
    UserRole Role,

    [Description("Parent user ID for Kid users")]
    string? ParentId,

    [Description("When the user was created")]
    [Required]
    DateTime CreatedAt,

    [Description("Who created this user")]
    [Required]
    string CreatedBy
);

/// <summary>
/// Response model for listing users
/// </summary>
/// <param name="Users">List of users</param>
/// <param name="TotalCount">Total number of users</param>
public record UsersListResponse(
    [Description("List of users")]
    [Required]
    List<UserResponse> Users,

    [Description("Total number of users")]
    [Required]
    int TotalCount
);
