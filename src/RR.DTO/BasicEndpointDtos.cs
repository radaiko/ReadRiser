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
