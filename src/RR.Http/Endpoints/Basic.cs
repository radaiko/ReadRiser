using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace RRHttp.Endpoints;

public static class Basic
{
    public static void MapBasicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1")
            .WithTags("Basic")
            .WithOpenApi();

        // Health check endpoint
        group.MapGet("/health", () => Results.Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        }))
        .WithName("GetHealth")
        .WithSummary("Health check endpoint")
        .WithDescription("Returns the health status of the API");

        // Status endpoint with available endpoints and version
        group.MapGet("/status", (IServiceProvider serviceProvider) =>
        {
            var apiDescriptionGroupCollectionProvider = serviceProvider.GetService<IApiDescriptionGroupCollectionProvider>();
            var endpoints = new List<object>();

            if (apiDescriptionGroupCollectionProvider != null)
            {
                var apiDescriptions = apiDescriptionGroupCollectionProvider.ApiDescriptionGroups.Items
                    .SelectMany(g => g.Items)
                    .Where(api => !string.IsNullOrEmpty(api.RelativePath))
                    .Select(api => new
                    {
                        Path = $"/{api.RelativePath}",
                        Method = api.HttpMethod,
                        Name = api.ActionDescriptor?.DisplayName
                    })
                    .Distinct()
                    .OrderBy(e => e.Path)
                    .ToList();

                endpoints.AddRange(apiDescriptions.Cast<object>());
            }

            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "1.0.0";

            return Results.Ok(new
            {
                ApplicationName = "RR.Http",
                Version = version,
                Timestamp = DateTime.UtcNow,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                AvailableEndpoints = endpoints
            });
        })
        .WithName("GetStatus")
        .WithSummary("API status and available endpoints")
        .WithDescription("Returns the API status, version, and list of available endpoints");
    }
}
