using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using RR.DTO;

namespace RRHttp.Endpoints;

public static class Basic
{
    public static void MapBasicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1")
            .WithTags("Basic")
            .WithOpenApi();

        // Health check endpoint
        group.MapGet("/health", () =>
        {
            var response = new HealthResponse(
                Status: "Healthy",
                Timestamp: DateTime.UtcNow,
                Environment: Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
            );
            return Results.Ok(response);
        })
        .WithName("GetHealth")
        .WithSummary("Check API Health")
        .WithDescription("Returns the health status of the API service. Use this endpoint to verify that the API is running and accessible.")
        .WithTags("Health")
        .Produces<HealthResponse>(StatusCodes.Status200OK, "application/json")
        .ProducesValidationProblem()
        .WithOpenApi();

        // Status endpoint with available endpoints and version
        group.MapGet("/status", (IServiceProvider serviceProvider) =>
        {
            var apiDescriptionGroupCollectionProvider = serviceProvider.GetService<IApiDescriptionGroupCollectionProvider>();
            var endpoints = new List<EndpointInfo>();

            if (apiDescriptionGroupCollectionProvider != null)
            {
                var apiDescriptions = apiDescriptionGroupCollectionProvider.ApiDescriptionGroups.Items
                    .SelectMany(g => g.Items)
                    .Where(api => !string.IsNullOrEmpty(api.RelativePath))
                    .Select(api => new EndpointInfo(
                        Path: $"/{api.RelativePath}",
                        Method: api.HttpMethod ?? "GET",
                        Name: api.ActionDescriptor?.DisplayName
                    ))
                    .Distinct()
                    .OrderBy(e => e.Path)
                    .ToList();

                endpoints.AddRange(apiDescriptions);
            }

            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "1.0.0";

            var response = new StatusResponse(
                ApplicationName: "RR.Http",
                Version: version,
                Timestamp: DateTime.UtcNow,
                Environment: Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                AvailableEndpoints: endpoints
            );

            return Results.Ok(response);
        })
        .WithName("GetStatus")
        .WithSummary("Get API Status and Available Endpoints")
        .WithDescription("Returns comprehensive API status information including version, environment, and automatically discovered endpoints. Useful for API discovery and monitoring.")
        .WithTags("Status", "Discovery")
        .Produces<StatusResponse>(StatusCodes.Status200OK, "application/json")
        .ProducesValidationProblem()
        .WithOpenApi();
    }
}
