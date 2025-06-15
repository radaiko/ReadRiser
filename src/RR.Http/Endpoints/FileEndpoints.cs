using Microsoft.AspNetCore.Mvc;
using RR.DTO;
using RR.Http.Services;

namespace RR.Http.Endpoints;

public static class FileEndpoints {
    public static void MapFileEndpoints(this IEndpointRouteBuilder app) {
        var group = app.MapGroup("/api/v1/files")
            .WithTags("Files")
            .WithOpenApi()
            .DisableAntiforgery(); // Required for file uploads

        // Upload file endpoint
        group.MapPost("/upload", async (
            IFormFile file,
            [FromHeader(Name = "X-User-ID")] string currentUserId,
            FileManagementService fileService) => {
                try {
                    if (file == null || file.Length == 0) {
                        return Results.BadRequest(new { error = "No file provided" });
                    }

                    var result = await fileService.UploadFileAsync(file, currentUserId);
                    return Results.Created($"/api/v1/files/{result.FileId}", result);
                } catch (UnauthorizedAccessException) {
                    return Results.Forbid();
                } catch (Exception ex) {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
        .WithName("UploadFile")
        .WithSummary("Upload a new file")
        .WithDescription("Uploads a file and stores its metadata. The file becomes owned by the uploading user.")
        .Accepts<IFormFile>("multipart/form-data")
        .Produces<UploadFileResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .WithOpenApi();

        // Get all files endpoint
        group.MapGet("/", (
            [FromHeader(Name = "X-User-ID")] string currentUserId,
            FileManagementService fileService) => {
                try {
                    var result = fileService.GetFilesForUser(currentUserId);
                    return Results.Ok(result);
                } catch (UnauthorizedAccessException) {
                    return Results.Forbid();
                }
            })
        .WithName("GetFiles")
        .WithSummary("Get all files accessible to the current user")
        .WithDescription("Returns files owned by the user or shared with them, respecting role-based access controls.")
        .Produces<FilesListResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden)
        .WithOpenApi();

        // Download file endpoint
        group.MapGet("/{id}/download", async (
            string id,
            [FromHeader(Name = "X-User-ID")] string currentUserId,
            FileManagementService fileService) => {
                try {
                    var (content, contentType, fileName) = await fileService.GetFileAsync(id, currentUserId);
                    return Results.File(content, contentType, fileName);
                } catch (FileNotFoundException) {
                    return Results.NotFound();
                } catch (UnauthorizedAccessException) {
                    return Results.Forbid();
                }
            })
        .WithName("DownloadFile")
        .WithSummary("Download a file")
        .WithDescription("Downloads a file if the current user has access to it.")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status403Forbidden)
        .WithOpenApi();

        // Get file metadata endpoint
        group.MapGet("/{id}", (
            string id,
            [FromHeader(Name = "X-User-ID")] string currentUserId,
            FileManagementService fileService) => {
                try {
                    // Get file metadata by checking if it's in the user's accessible files
                    var filesResponse = fileService.GetFilesForUser(currentUserId);
                    var file = filesResponse.Files.FirstOrDefault(f => f.Id == id);

                    if (file == null) {
                        return Results.NotFound();
                    }

                    return Results.Ok(file);
                } catch (UnauthorizedAccessException) {
                    return Results.Forbid();
                }
            })
        .WithName("GetFileMetadata")
        .WithSummary("Get file metadata")
        .WithDescription("Returns detailed metadata for a file including sharing history and permissions.")
        .Produces<FileMetadata>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status403Forbidden)
        .WithOpenApi();

        // Share file endpoint
        group.MapPost("/{id}/share", (
            string id,
            [FromBody] ShareFileRequest request,
            [FromHeader(Name = "X-User-ID")] string currentUserId,
            FileManagementService fileService) => {
                try {
                    // Update the request with the file ID from the URL
                    var shareRequest = request with { FileId = id };
                    var result = fileService.ShareFile(shareRequest, currentUserId);
                    return Results.Ok(result);
                } catch (FileNotFoundException) {
                    return Results.NotFound();
                } catch (UnauthorizedAccessException) {
                    return Results.Forbid();
                } catch (ArgumentException ex) {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
        .WithName("ShareFile")
        .WithSummary("Share a file with other users")
        .WithDescription("Shares a file with specified users based on role permissions. Parents can share with their Kids and other Parents, Kids can share with other Kids.")
        .Produces<ShareFileResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status403Forbidden)
        .WithOpenApi();
    }
}
