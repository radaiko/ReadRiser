using Microsoft.AspNetCore.Http;
using RR.Core.Interfaces;
using RR.Core.Models;
using RR.DTO;

namespace RR.Core.Services;

/// <summary>
/// Service for file management with role-based access control and sharing logic
/// </summary>
public class FileManagementService : IFileManagementService {
    private readonly IFileBasedDbService _dbService;
    private readonly IFileStorageService _storageService;

    public FileManagementService(IFileBasedDbService dbService, IFileStorageService storageService) {
        _dbService = dbService;
        _storageService = storageService;
    }

    public async Task<UploadFileResponse> UploadFileAsync(IFormFile file, string uploaderId) {
        var uploader = _dbService.GetUserById(uploaderId);
        if (uploader == null) {
            throw new UnauthorizedAccessException("Uploader user not found");
        }

        var fileId = Guid.NewGuid().ToString();
        var storagePath = await _storageService.SaveFileAsync(file, fileId);

        var fileEntity = new FileEntity {
            Id = fileId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Size = file.Length,
            UploaderId = uploaderId,
            OwnerId = uploaderId,
            SharedWith = new List<string>(),
            SharingHistory = new List<SharingHistoryEntryInternal>(),
            UploadedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            StoragePath = storagePath
        };

        _dbService.SaveFile(fileEntity);

        return new UploadFileResponse(
            FileId: fileId,
            FileName: file.FileName,
            Size: file.Length,
            ContentType: file.ContentType,
            UploadedAt: DateTime.UtcNow
        );
    }

    public async Task<(byte[] content, string contentType, string fileName)> GetFileAsync(string fileId, string userId) {
        var user = _dbService.GetUserById(userId);
        if (user == null) {
            throw new UnauthorizedAccessException("User not found");
        }

        var file = _dbService.GetFileById(fileId);
        if (file == null) {
            throw new FileNotFoundException("File not found");
        }

        if (!CanUserAccessFile(user, file)) {
            throw new UnauthorizedAccessException("Access denied to this file");
        }

        var (content, contentType) = await _storageService.GetFileAsync(file.StoragePath, file.ContentType);
        return (content, contentType, file.FileName);
    }

    public FilesListResponse GetFilesForUser(string userId) {
        var user = _dbService.GetUserById(userId);
        if (user == null) {
            throw new UnauthorizedAccessException("User not found");
        }

        var allFiles = _dbService.GetFiles();
        var accessibleFiles = allFiles.Where(f => CanUserAccessFile(user, f)).ToList();

        var filesMetadata = accessibleFiles.Select(ConvertToFileMetadata).ToList();

        return new FilesListResponse(
            Files: filesMetadata,
            TotalCount: filesMetadata.Count
        );
    }

    public ShareFileResponse ShareFile(ShareFileRequest request, string sharerId) {
        var sharer = _dbService.GetUserById(sharerId);
        if (sharer == null) {
            throw new UnauthorizedAccessException("Sharer user not found");
        }

        var file = _dbService.GetFileById(request.FileId);
        if (file == null) {
            throw new FileNotFoundException("File not found");
        }

        // Check if user can share this file (must own it or have it shared with them)
        if (!CanUserAccessFile(sharer, file)) {
            throw new UnauthorizedAccessException("Access denied to this file");
        }

        var validTargetUsers = new List<string>();
        var sharingTime = DateTime.UtcNow;

        foreach (var targetUserId in request.UserIds) {
            var targetUser = _dbService.GetUserById(targetUserId);
            if (targetUser == null) {
                continue; // Skip invalid users
            }

            // Validate sharing permissions based on roles
            if (CanUserShareWithUser(sharer, targetUser)) {
                validTargetUsers.Add(targetUserId);

                // Add to shared list if not already shared
                if (!file.SharedWith.Contains(targetUserId)) {
                    file.SharedWith.Add(targetUserId);
                }

                // Add to sharing history
                var historyEntry = new SharingHistoryEntryInternal {
                    Id = Guid.NewGuid().ToString(),
                    SharedBy = sharerId,
                    SharedWith = targetUserId,
                    SharedAt = sharingTime,
                    ParentShareId = null // Could be used for tracking sharing chains
                };

                file.SharingHistory.Add(historyEntry);
            }
        }

        if (validTargetUsers.Any()) {
            file.LastModified = sharingTime;
            _dbService.SaveFile(file);
        }

        return new ShareFileResponse(
            FileId: request.FileId,
            SharedWith: validTargetUsers,
            SharedAt: sharingTime
        );
    }

    private bool CanUserAccessFile(User user, FileEntity file) {
        // Owner can always access
        if (file.OwnerId == user.Id) {
            return true;
        }

        // File is shared with user
        if (file.SharedWith.Contains(user.Id)) {
            return true;
        }

        // Admin can access all files
        if (user.Role == UserRole.Admin) {
            return true;
        }

        // Parents can access files owned by their kids
        if (user.Role == UserRole.Parent) {
            var fileOwner = _dbService.GetUserById(file.OwnerId);
            if (fileOwner?.Role == UserRole.Kid && fileOwner.ParentId == user.Id) {
                return true;
            }
        }

        return false;
    }

    private static bool CanUserShareWithUser(User sharer, User target) {
        switch (sharer.Role) {
            case UserRole.Admin:
                return true; // Admins can share with anyone

            case UserRole.Parent:
                // Parents can share with their kids and other parents
                return target.Role == UserRole.Parent ||
                       (target.Role == UserRole.Kid && target.ParentId == sharer.Id);

            case UserRole.Kid:
                // Kids can share with other kids only
                return target.Role == UserRole.Kid;

            default:
                return false;
        }
    }

    private static DTO.FileMetadata ConvertToFileMetadata(FileEntity file) {
        var sharingHistory = file.SharingHistory.Select(sh => new DTO.SharingHistoryEntry(
            SharedBy: sh.SharedBy,
            SharedWith: sh.SharedWith,
            SharedAt: sh.SharedAt,
            ParentShareId: sh.ParentShareId
        )).ToList();

        return new DTO.FileMetadata(
            Id: file.Id,
            FileName: file.FileName,
            ContentType: file.ContentType,
            Size: file.Size,
            UploaderId: file.UploaderId,
            OwnerId: file.OwnerId,
            SharedWith: file.SharedWith,
            SharingHistory: sharingHistory,
            UploadedAt: file.UploadedAt,
            LastModified: file.LastModified
        );
    }
}
