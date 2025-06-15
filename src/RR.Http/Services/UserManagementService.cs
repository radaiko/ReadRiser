using RR.DTO;
using RR.Http.Models;

namespace RR.Http.Services;

/// <summary>
/// Service for user management with role-based access control
/// </summary>
public class UserManagementService {
    private readonly FileBasedDbService _dbService;

    public UserManagementService(FileBasedDbService dbService) {
        _dbService = dbService;
    }

    public UserResponse CreateUser(CreateUserRequest request, string createdById) {
        var createdBy = _dbService.GetUserById(createdById);
        if (createdBy == null) {
            throw new UnauthorizedAccessException("Creator user not found");
        }

        // Validate role creation permissions
        ValidateRoleCreationPermissions(createdBy.Role, request.Role);

        // Validate parent relationship for Kid users
        if (request.Role == UserRole.Kid) {
            if (string.IsNullOrEmpty(request.ParentId)) {
                throw new ArgumentException("ParentId is required for Kid users");
            }

            var parent = _dbService.GetUserById(request.ParentId);
            if (parent == null || parent.Role != UserRole.Parent) {
                throw new ArgumentException("Invalid parent user");
            }

            // Only the parent can create their own kids, or admins can create kids for any parent
            if (createdBy.Role == UserRole.Parent && createdBy.Id != request.ParentId) {
                throw new UnauthorizedAccessException("Parents can only create their own Kids");
            }
        }

        // Check if username already exists
        var existingUser = _dbService.GetUserByUsername(request.Username);
        if (existingUser != null) {
            throw new ArgumentException("Username already exists");
        }

        var user = new User {
            Id = Guid.NewGuid().ToString(),
            Username = request.Username,
            DisplayName = request.DisplayName,
            Role = request.Role,
            ParentId = request.ParentId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdById
        };

        _dbService.SaveUser(user);

        // Update parent's children list if this is a Kid
        if (request.Role == UserRole.Kid && !string.IsNullOrEmpty(request.ParentId)) {
            var parent = _dbService.GetUserById(request.ParentId);
            if (parent != null) {
                parent.ChildrenIds.Add(user.Id);
                _dbService.SaveUser(parent);
            }
        }

        return ConvertToUserResponse(user);
    }

    public List<UserResponse> GetUsersForUser(string userId) {
        var currentUser = _dbService.GetUserById(userId);
        if (currentUser == null) {
            throw new UnauthorizedAccessException("User not found");
        }

        var allUsers = _dbService.GetUsers();
        var visibleUsers = new List<User>();

        switch (currentUser.Role) {
            case UserRole.Admin:
                // Admins can see all users
                visibleUsers = allUsers;
                break;

            case UserRole.Parent:
                // Parents can see themselves, their kids, and other parents
                visibleUsers = allUsers.Where(u =>
                    u.Role == UserRole.Admin ||
                    u.Role == UserRole.Parent ||
                    (u.Role == UserRole.Kid && u.ParentId == currentUser.Id))
                    .ToList();
                break;

            case UserRole.Kid:
                // Kids can see their parent, themselves, and other kids
                visibleUsers = allUsers.Where(u =>
                    u.Id == currentUser.Id ||
                    u.Id == currentUser.ParentId ||
                    u.Role == UserRole.Kid)
                    .ToList();
                break;
        }

        return visibleUsers.Select(ConvertToUserResponse).ToList();
    }

    public UserResponse? GetUserById(string userId, string requesterId) {
        var requester = _dbService.GetUserById(requesterId);
        if (requester == null) {
            throw new UnauthorizedAccessException("Requester user not found");
        }

        var targetUser = _dbService.GetUserById(userId);
        if (targetUser == null) {
            return null;
        }

        // Check if requester can access this user
        if (!CanUserAccessUser(requester, targetUser)) {
            throw new UnauthorizedAccessException("Access denied to this user");
        }

        return ConvertToUserResponse(targetUser);
    }

    private static void ValidateRoleCreationPermissions(UserRole creatorRole, UserRole targetRole) {
        switch (creatorRole) {
            case UserRole.Admin:
                if (targetRole != UserRole.Parent && targetRole != UserRole.Kid) {
                    throw new UnauthorizedAccessException("Admins can only create Parent and Kid users");
                }
                break;

            case UserRole.Parent:
                if (targetRole != UserRole.Kid) {
                    throw new UnauthorizedAccessException("Parents can only create Kid users");
                }
                break;

            case UserRole.Kid:
                throw new UnauthorizedAccessException("Kids cannot create users");

            default:
                throw new UnauthorizedAccessException("Invalid creator role");
        }
    }

    private static bool CanUserAccessUser(User requester, User target) {
        switch (requester.Role) {
            case UserRole.Admin:
                return true; // Admins can access all users

            case UserRole.Parent:
                return target.Role == UserRole.Admin ||
                       target.Role == UserRole.Parent ||
                       (target.Role == UserRole.Kid && target.ParentId == requester.Id);

            case UserRole.Kid:
                return target.Id == requester.Id ||
                       target.Id == requester.ParentId ||
                       target.Role == UserRole.Kid;

            default:
                return false;
        }
    }

    private static UserResponse ConvertToUserResponse(User user) {
        return new UserResponse(
            Id: user.Id,
            Username: user.Username,
            DisplayName: user.DisplayName,
            Role: user.Role,
            ParentId: user.ParentId,
            CreatedAt: user.CreatedAt,
            CreatedBy: user.CreatedBy
        );
    }
}
