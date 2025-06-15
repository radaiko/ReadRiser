using Microsoft.AspNetCore.Http;

namespace RR.Core.Interfaces;

/// <summary>
/// Interface for file storage operations
/// </summary>
public interface IFileStorageService {
    Task<string> SaveFileAsync(IFormFile file, string fileId);
    Task<(byte[] content, string contentType)> GetFileAsync(string storagePath, string contentType);
    void DeleteFile(string storagePath);
    bool FileExists(string storagePath);
}
