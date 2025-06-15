using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using RR.Core.Interfaces;
using RR.DTO;

namespace RR.Http.Endpoints;

public static class UserEndpoints {
    public static void MapUserEndpoints(this IEndpointRouteBuilder app) {
        var group = app.MapGroup("/api/v1/users")
            .WithTags("Users")
            .WithOpenApi();

        // Create user endpoint
        group.MapPost("/", (
            [FromBody] CreateUserRequest request,
            [FromHeader(Name = "X-User-ID")] string currentUserId,
            IUserManagementService userService) => {
                try {
                    var result = userService.CreateUser(request, currentUserId);
                    return Results.Created($"/api/v1/users/{result.Id}", result);
                } catch (UnauthorizedAccessException) {
                    return Results.Forbid();
                } catch (ArgumentException ex) {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
        .WithName("CreateUser")
        .WithSummary("Create a new user")
        .WithDescription("Creates a new user with role-based restrictions. Admins can create Parents and Kids, Parents can create Kids only.")
        .Produces<UserResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .WithOpenApi();

        // Get all users endpoint
        group.MapGet("/", (
            [FromHeader(Name = "X-User-ID")] string currentUserId,
            IUserManagementService userService) => {
                try {
                    var users = userService.GetUsersForUser(currentUserId);
                    var response = new UsersListResponse(users, users.Count);
                    return Results.Ok(response);
                } catch (UnauthorizedAccessException) {
                    return Results.Forbid();
                }
            })
        .WithName("GetUsers")
        .WithSummary("Get all users accessible to the current user")
        .WithDescription("Returns users based on role permissions. Admins see all, Parents see other Parents and their Kids, Kids see their Parent and other Kids.")
        .Produces<UsersListResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden)
        .WithOpenApi();

        // Get user by ID endpoint
        group.MapGet("/{id}", (
            string id,
            [FromHeader(Name = "X-User-ID")] string currentUserId,
            IUserManagementService userService) => {
                try {
                    var user = userService.GetUserById(id, currentUserId);
                    if (user == null) {
                        return Results.NotFound();
                    }
                    return Results.Ok(user);
                } catch (UnauthorizedAccessException) {
                    return Results.Forbid();
                }
            })
        .WithName("GetUserById")
        .WithSummary("Get a specific user by ID")
        .WithDescription("Returns user details if the current user has permission to access them.")
        .Produces<UserResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status403Forbidden)
        .WithOpenApi();
    }
}
