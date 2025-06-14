using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace RR.DTO;

/// <summary>
/// Response model for health check endpoint
/// </summary>
/// <param name="Status">The health status of the API (e.g., "Healthy", "Unhealthy", "Degraded")</param>
/// <param name="Timestamp">The timestamp when the health check was performed in UTC</param>
/// <param name="Environment">The current environment (Development, Staging, Production, etc.)</param>
/// <example>
/// {
///   "status": "Healthy",
///   "timestamp": "2025-06-14T10:30:00Z",
///   "environment": "Production"
/// }
/// </example>
/// <remarks>
/// This endpoint is typically used by monitoring systems and load balancers
/// to verify that the API service is operational and ready to handle requests.
/// </remarks>
public record HealthResponse(
    [Description("The current health status of the API service")]
    [Required]
    string Status,

    [Description("UTC timestamp when the health check was performed")]
    [Required]
    DateTime Timestamp,

    [Description("The deployment environment name")]
    [Required]
    [StringLength(50, MinimumLength = 1)]
    string Environment
);

/// <summary>
/// Information about an available API endpoint
/// </summary>
/// <param name="Path">The endpoint path/route (e.g., "/api/v1/health")</param>
/// <param name="Method">The HTTP method (GET, POST, PUT, DELETE, PATCH, etc.)</param>
/// <param name="Name">The endpoint name/identifier used for routing and documentation</param>
/// <example>
/// {
///   "path": "/api/v1/health",
///   "method": "GET",
///   "name": "GetHealth"
/// }
/// </example>
/// <remarks>
/// This model represents metadata about API endpoints that can be discovered
/// programmatically. It's useful for building dynamic API clients or documentation.
/// </remarks>
public record EndpointInfo(
    [Description("The full path/route of the endpoint including any route prefixes")]
    [Required]
    [StringLength(500, MinimumLength = 1)]
    string Path,

    [Description("The HTTP method/verb used to access this endpoint")]
    [Required]
    [StringLength(10, MinimumLength = 3)]
    string Method,

    [Description("Optional friendly name or identifier for the endpoint")]
    [StringLength(100)]
    string? Name
);

/// <summary>
/// Response model for status endpoint containing comprehensive API information and available endpoints
/// </summary>
/// <param name="ApplicationName">The name of the application/service</param>
/// <param name="Version">The application version following semantic versioning (e.g., "1.2.3")</param>
/// <param name="Timestamp">The timestamp when the status was retrieved in UTC</param>
/// <param name="Environment">The current deployment environment</param>
/// <param name="AvailableEndpoints">Complete list of all discoverable API endpoints with their metadata</param>
/// <example>
/// {
///   "applicationName": "RR.Http",
///   "version": "1.0.0",
///   "timestamp": "2025-06-14T10:30:00Z",
///   "environment": "Production",
///   "availableEndpoints": [
///     {
///       "path": "/api/v1/health",
///       "method": "GET",
///       "name": "GetHealth"
///     },
///     {
///       "path": "/api/v1/status",
///       "method": "GET", 
///       "name": "GetStatus"
///     }
///   ]
/// }
/// </example>
/// <remarks>
/// This comprehensive status response provides everything needed for API discovery,
/// monitoring, and client generation. It includes both static metadata (name, version)
/// and dynamic endpoint discovery to help clients understand available functionality.
/// The endpoint list is automatically generated from the application's route configuration.
/// </remarks>
public record StatusResponse(
    [Description("The name of the application or microservice")]
    [Required]
    [StringLength(100, MinimumLength = 1)]
    string ApplicationName,

    [Description("The current version of the application following semantic versioning")]
    [Required]
    [StringLength(20, MinimumLength = 1)]
    string Version,

    [Description("UTC timestamp when this status information was generated")]
    [Required]
    DateTime Timestamp,

    [Description("The deployment environment name")]
    [Required]
    [StringLength(50, MinimumLength = 1)]
    string Environment,

    [Description("Comprehensive list of all available API endpoints with their metadata")]
    [Required]
    List<EndpointInfo> AvailableEndpoints
);

/// <summary>
/// Information about a NuGet package and its license
/// </summary>
/// <param name="Name">The name of the NuGet package</param>
/// <param name="Version">The version of the package currently in use</param>
/// <param name="LicenseType">The type of license (e.g., "MIT", "Apache-2.0", "BSD-3-Clause")</param>
/// <param name="LicenseUrl">URL to the license text, if available</param>
/// <param name="ProjectUrl">URL to the project homepage or repository</param>
/// <param name="Authors">The authors or maintainers of the package</param>
/// <param name="Description">Brief description of what the package does</param>
/// <example>
/// {
///   "name": "Microsoft.AspNetCore.OpenApi",
///   "version": "9.0.0",
///   "licenseType": "MIT",
///   "licenseUrl": "https://licenses.nuget.org/MIT",
///   "projectUrl": "https://github.com/dotnet/aspnetcore",
///   "authors": "Microsoft",
///   "description": "Provides OpenAPI specification generation for ASP.NET Core applications"
/// }
/// </example>
/// <remarks>
/// This model contains essential legal and attribution information for third-party packages
/// used in the application. Including this information helps ensure license compliance
/// and provides transparency about dependencies.
/// </remarks>
public record PackageInfo(
    [Description("The name of the NuGet package")]
    [Required]
    [StringLength(200, MinimumLength = 1)]
    string Name,

    [Description("The version of the package currently in use")]
    [Required]
    [StringLength(50, MinimumLength = 1)]
    string Version,

    [Description("The type of license under which the package is distributed")]
    [StringLength(100)]
    string? LicenseType,

    [Description("URL to the full license text")]
    [StringLength(500)]
    string? LicenseUrl,

    [Description("URL to the project homepage or source repository")]
    [StringLength(500)]
    string? ProjectUrl,

    [Description("The authors or maintainers of the package")]
    [StringLength(500)]
    string? Authors,

    [Description("Brief description of the package's functionality")]
    [StringLength(1000)]
    string? Description
);

/// <summary>
/// Response model for credits endpoint containing all third-party package information
/// </summary>
/// <param name="ApplicationName">The name of the application</param>
/// <param name="ApplicationVersion">The version of the application</param>
/// <param name="GeneratedAt">When this credits information was generated</param>
/// <param name="Packages">List of all third-party packages with their license information</param>
/// <param name="TotalPackages">Total number of packages included</param>
/// <example>
/// {
///   "applicationName": "ReadRiser API",
///   "applicationVersion": "1.0.0",
///   "generatedAt": "2025-06-14T10:30:00Z",
///   "packages": [
///     {
///       "name": "Microsoft.AspNetCore.OpenApi",
///       "version": "9.0.0",
///       "licenseType": "MIT",
///       "licenseUrl": "https://licenses.nuget.org/MIT",
///       "projectUrl": "https://github.com/dotnet/aspnetcore",
///       "authors": "Microsoft",
///       "description": "Provides OpenAPI specification generation for ASP.NET Core applications"
///     }
///   ],
///   "totalPackages": 1
/// }
/// </example>
/// <remarks>
/// This endpoint provides comprehensive attribution and license information for all
/// third-party packages used in the application. This information is essential for
/// legal compliance, especially in commercial applications where license requirements
/// must be met. The data helps developers and legal teams understand the licensing
/// obligations and attribution requirements for the application.
/// </remarks>
public record CreditsResponse(
    [Description("The name of the application")]
    [Required]
    [StringLength(100, MinimumLength = 1)]
    string ApplicationName,

    [Description("The current version of the application")]
    [Required]
    [StringLength(50, MinimumLength = 1)]
    string ApplicationVersion,

    [Description("UTC timestamp when this credits information was generated")]
    [Required]
    DateTime GeneratedAt,

    [Description("List of all third-party packages with their license and attribution information")]
    [Required]
    List<PackageInfo> Packages,

    [Description("Total number of packages included in the credits")]
    [Required]
    [Range(0, int.MaxValue)]
    int TotalPackages
);
