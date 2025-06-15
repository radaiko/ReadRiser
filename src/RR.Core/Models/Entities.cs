using RR.DTO;

namespace RR.Core.Models;

/// <summary>
/// User entity for file-based storage
/// </summary>
public class User {
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? ParentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public List<string> ChildrenIds { get; set; } = new();
}

/// <summary>
/// File entity for file-based storage
/// </summary>
public class FileEntity {
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public string UploaderId { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public List<string> SharedWith { get; set; } = new();
    public List<SharingHistoryEntryInternal> SharingHistory { get; set; } = new();
    public DateTime UploadedAt { get; set; }
    public DateTime LastModified { get; set; }
    public string StoragePath { get; set; } = string.Empty;
}

/// <summary>
/// Internal sharing history entry for persistence
/// </summary>
public class SharingHistoryEntryInternal {
    public string Id { get; set; } = string.Empty;
    public string SharedBy { get; set; } = string.Empty;
    public string SharedWith { get; set; } = string.Empty;
    public DateTime SharedAt { get; set; }
    public string? ParentShareId { get; set; }
}
