using RR.DTO;

namespace RR.Core.Interfaces;

/// <summary>
/// Interface for user management operations
/// </summary>
public interface IUserManagementService {
    UserResponse CreateUser(CreateUserRequest request, string createdById);
    List<UserResponse> GetUsersForUser(string userId);
    UserResponse? GetUserById(string userId, string requesterId);
}
