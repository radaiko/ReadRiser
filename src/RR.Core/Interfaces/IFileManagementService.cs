using Microsoft.AspNetCore.Http;
using RR.DTO;

namespace RR.Core.Interfaces;

/// <summary>
/// Interface for file management operations
/// </summary>
public interface IFileManagementService {
    Task<UploadFileResponse> UploadFileAsync(IFormFile file, string uploaderId);
    Task<(byte[] content, string contentType, string fileName)> GetFileAsync(string fileId, string userId);
    FilesListResponse GetFilesForUser(string userId);
    ShareFileResponse ShareFile(ShareFileRequest request, string sharerId);
}
