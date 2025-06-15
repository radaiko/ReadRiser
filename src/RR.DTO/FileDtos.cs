using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RR.DTO;

/// <summary>
/// Sharing history entry tracking who shared a file with whom
/// </summary>
/// <param name="SharedBy">User who shared the file</param>
/// <param name="SharedWith">User who received the file</param>
/// <param name="SharedAt">When the sharing occurred</param>
/// <param name="ParentShareId">Reference to the parent share in the chain (if any)</param>
public record SharingHistoryEntry(
    [Description("User who shared the file")]
    [Required]
    string SharedBy,

    [Description("User who received the file")]
    [Required]
    string SharedWith,

    [Description("When the sharing occurred")]
    [Required]
    DateTime SharedAt,

    [Description("Reference to the parent share in the chain")]
    string? ParentShareId
);

/// <summary>
/// File metadata containing all file information
/// </summary>
/// <param name="Id">Unique identifier for the file</param>
/// <param name="FileName">Original filename</param>
/// <param name="ContentType">MIME type of the file</param>
/// <param name="Size">File size in bytes</param>
/// <param name="UploaderId">User who uploaded the file</param>
/// <param name="OwnerId">Current owner of the file</param>
/// <param name="SharedWith">List of users the file is currently shared with</param>
/// <param name="SharingHistory">Complete sharing history for audit trail</param>
/// <param name="UploadedAt">When the file was uploaded</param>
/// <param name="LastModified">When the file was last modified</param>
public record FileMetadata(
    [Description("Unique identifier for the file")]
    [Required]
    string Id,

    [Description("Original filename")]
    [Required]
    string FileName,

    [Description("MIME type of the file")]
    [Required]
    string ContentType,

    [Description("File size in bytes")]
    [Required]
    long Size,

    [Description("User who uploaded the file")]
    [Required]
    string UploaderId,

    [Description("Current owner of the file")]
    [Required]
    string OwnerId,

    [Description("List of users the file is currently shared with")]
    [Required]
    List<string> SharedWith,

    [Description("Complete sharing history for audit trail")]
    [Required]
    List<SharingHistoryEntry> SharingHistory,

    [Description("When the file was uploaded")]
    [Required]
    DateTime UploadedAt,

    [Description("When the file was last modified")]
    [Required]
    DateTime LastModified
);

/// <summary>
/// Request model for uploading a file
/// </summary>
/// <param name="File">The file to upload</param>
public record UploadFileRequest(
    [Description("The file to upload")]
    [Required]
    IFormFile File
);

/// <summary>
/// Response model for file upload
/// </summary>
/// <param name="FileId">Unique identifier of the uploaded file</param>
/// <param name="FileName">Original filename</param>
/// <param name="Size">File size in bytes</param>
/// <param name="ContentType">MIME type of the file</param>
/// <param name="UploadedAt">When the file was uploaded</param>
public record UploadFileResponse(
    [Description("Unique identifier of the uploaded file")]
    [Required]
    string FileId,

    [Description("Original filename")]
    [Required]
    string FileName,

    [Description("File size in bytes")]
    [Required]
    long Size,

    [Description("MIME type of the file")]
    [Required]
    string ContentType,

    [Description("When the file was uploaded")]
    [Required]
    DateTime UploadedAt
);

/// <summary>
/// Request model for sharing a file
/// </summary>
/// <param name="FileId">ID of the file to share</param>
/// <param name="UserIds">List of user IDs to share the file with</param>
public record ShareFileRequest(
    [Description("ID of the file to share")]
    string FileId,

    [Description("List of user IDs to share the file with")]
    [Required]
    [MinLength(1)]
    List<string> UserIds
);

/// <summary>
/// Response model for file sharing operation
/// </summary>
/// <param name="FileId">ID of the shared file</param>
/// <param name="SharedWith">List of users the file was shared with</param>
/// <param name="SharedAt">When the sharing occurred</param>
public record ShareFileResponse(
    [Description("ID of the shared file")]
    [Required]
    string FileId,

    [Description("List of users the file was shared with")]
    [Required]
    List<string> SharedWith,

    [Description("When the sharing occurred")]
    [Required]
    DateTime SharedAt
);

/// <summary>
/// Response model for listing files
/// </summary>
/// <param name="Files">List of files accessible to the user</param>
/// <param name="TotalCount">Total number of accessible files</param>
public record FilesListResponse(
    [Description("List of files accessible to the user")]
    [Required]
    List<FileMetadata> Files,

    [Description("Total number of accessible files")]
    [Required]
    int TotalCount
);
