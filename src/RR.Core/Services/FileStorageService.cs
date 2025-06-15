using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using RR.Core.Interfaces;

namespace RR.Core.Services;

/// <summary>
/// Service for handling physical file storage operations
/// </summary>
public class FileStorageService : IFileStorageService {
    private readonly string _storagePath;

    public FileStorageService(IConfiguration configuration) {
        var configuredPath = configuration["FileStorage:FilesPath"] ?? "uploads";
        _storagePath = Path.GetFullPath(configuredPath);
        EnsureStorageDirectoryExists();
    }

    private void EnsureStorageDirectoryExists() {
        if (!Directory.Exists(_storagePath)) {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task<string> SaveFileAsync(IFormFile file, string fileId) {
        var fileName = $"{fileId}_{file.FileName}";
        var filePath = Path.Combine(_storagePath, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return filePath;
    }

    public async Task<(byte[] content, string contentType)> GetFileAsync(string storagePath, string contentType) {
        if (!File.Exists(storagePath)) {
            throw new FileNotFoundException("File not found", storagePath);
        }

        var content = await File.ReadAllBytesAsync(storagePath);
        return (content, contentType);
    }

    public void DeleteFile(string storagePath) {
        if (File.Exists(storagePath)) {
            File.Delete(storagePath);
        }
    }

    public bool FileExists(string storagePath) {
        return File.Exists(storagePath);
    }
}
