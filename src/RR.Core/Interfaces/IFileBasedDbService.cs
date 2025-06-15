using RR.Core.Models;

namespace RR.Core.Interfaces;

/// <summary>
/// Interface for file-based database operations
/// </summary>
public interface IFileBasedDbService {
    // User operations
    List<User> GetUsers();
    User? GetUserById(string id);
    User? GetUserByUsername(string username);
    void SaveUser(User user);
    void DeleteUser(string id);

    // File operations
    List<FileEntity> GetFiles();
    FileEntity? GetFileById(string id);
    List<FileEntity> GetFilesByOwner(string ownerId);
    List<FileEntity> GetFilesSharedWithUser(string userId);
    void SaveFile(FileEntity file);
    void DeleteFile(string id);
}
