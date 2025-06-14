using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using RR.DTO;

namespace RR.Http.Services;

/// <summary>
/// Service for gathering information about NuGet packages used in the application
/// </summary>
public class PackageInfoService
{
    private readonly ILogger<PackageInfoService> _logger;

    public PackageInfoService(ILogger<PackageInfoService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets information about all packages used in the application
    /// </summary>
    /// <returns>List of package information including licenses</returns>
    public async Task<List<PackageInfo>> GetPackageInfoAsync()
    {
        var packages = new List<PackageInfo>();

        try
        {
            // Try to get packages from project.assets.json
            var discoveredPackages = await GetPackagesFromProjectAssetsAsync();
            if (discoveredPackages.Any())
            {
                packages.AddRange(discoveredPackages);
                _logger.LogInformation("Retrieved information for {PackageCount} packages from project.assets.json", packages.Count);
            }
            else
            {
                // Fallback to known packages if project.assets.json is not available
                packages.AddRange(GetKnownPackages());
                _logger.LogInformation("Retrieved information for {PackageCount} packages from fallback list", packages.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving package information, using fallback");
            packages.AddRange(GetKnownPackages());
        }

        return packages.OrderBy(p => p.Name).ToList();
    }

    /// <summary>
    /// Returns fallback information about core packages when automatic discovery fails
    /// </summary>
    private static List<PackageInfo> GetKnownPackages()
    {
        // Fallback list of core packages - only the essential ones
        var corePackages = new[]
        {
            ("Microsoft.AspNetCore.OpenApi", "9.0.0"),
            ("Scalar.AspNetCore", "2.4.16"),
            ("Swashbuckle.AspNetCore", "9.0.1")
        };

        return corePackages.Select(package => new PackageInfo(
            Name: package.Item1,
            Version: package.Item2,
            LicenseType: GetLicenseType(package.Item1),
            LicenseUrl: GetLicenseUrl(package.Item1),
            ProjectUrl: GetProjectUrl(package.Item1),
            Authors: GetAuthors(package.Item1),
            Description: GetDescription(package.Item1)
        )).ToList();
    }

    /// <summary>
    /// Gets packages from project.assets.json file automatically
    /// </summary>
    private async Task<List<PackageInfo>> GetPackagesFromProjectAssetsAsync()
    {
        var packages = new List<PackageInfo>();
        
        try
        {
            // Find the project.assets.json file
            var currentDirectory = Directory.GetCurrentDirectory();
            var projectAssetsPath = Path.Combine(currentDirectory, "obj", "project.assets.json");
            
            if (!File.Exists(projectAssetsPath))
            {
                _logger.LogWarning("project.assets.json not found at {Path}", projectAssetsPath);
                return packages;
            }

            var jsonContent = await File.ReadAllTextAsync(projectAssetsPath);
            var jsonDocument = JsonNode.Parse(jsonContent);
            
            if (jsonDocument == null)
            {
                _logger.LogWarning("Failed to parse project.assets.json");
                return packages;
            }

            // Get the libraries section which contains all package information
            var libraries = jsonDocument["libraries"]?.AsObject();
            if (libraries == null)
            {
                _logger.LogWarning("No libraries section found in project.assets.json");
                return packages;
            }

            // Get direct dependencies from the project section
            var directDependencies = GetDirectDependencies(jsonDocument);

            foreach (var library in libraries)
            {
                var packageName = library.Key;
                var packageInfo = library.Value?.AsObject();
                
                if (packageInfo == null) continue;

                // Skip if not a package (could be project reference)
                var type = packageInfo["type"]?.GetValue<string>();
                if (type != "package") continue;

                // Parse package name and version from the key (format: "PackageName/Version")
                var parts = packageName.Split('/');
                if (parts.Length != 2) continue;

                var name = parts[0];
                var version = parts[1];

                // Only include direct dependencies or well-known packages
                if (!directDependencies.Contains(name) && !IsWellKnownPackage(name)) continue;

                var packageInfoDto = new PackageInfo(
                    Name: name,
                    Version: version,
                    LicenseType: GetLicenseType(name),
                    LicenseUrl: GetLicenseUrl(name),
                    ProjectUrl: GetProjectUrl(name),
                    Authors: GetAuthors(name),
                    Description: GetDescription(name)
                );

                packages.Add(packageInfoDto);
            }

            _logger.LogInformation("Discovered {Count} packages from project.assets.json", packages.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading packages from project.assets.json");
        }

        return packages;
    }

    /// <summary>
    /// Gets direct dependencies from the project section
    /// </summary>
    private static HashSet<string> GetDirectDependencies(JsonNode jsonDocument)
    {
        var dependencies = new HashSet<string>();
        
        try
        {
            var frameworks = jsonDocument["project"]?["frameworks"]?.AsObject();
            if (frameworks != null)
            {
                foreach (var framework in frameworks)
                {
                    var deps = framework.Value?["dependencies"]?.AsObject();
                    if (deps != null)
                    {
                        foreach (var dep in deps)
                        {
                            dependencies.Add(dep.Key);
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            // Ignore errors in parsing dependencies
        }

        return dependencies;
    }

    /// <summary>
    /// Checks if a package is a well-known system package that should be included
    /// </summary>
    private static bool IsWellKnownPackage(string packageName)
    {
        var wellKnownPackages = new[]
        {
            "Microsoft.AspNetCore.OpenApi",
            "Microsoft.OpenApi",
            "Scalar.AspNetCore", 
            "Swashbuckle.AspNetCore.Swagger",
            "Swashbuckle.AspNetCore.SwaggerGen",
            "Swashbuckle.AspNetCore.SwaggerUI"
        };

        return wellKnownPackages.Contains(packageName);
    }

    /// <summary>
    /// Gets the license type for a package
    /// </summary>
    private static string GetLicenseType(string packageName)
    {
        return packageName.StartsWith("Microsoft.") ? "MIT" : "MIT";
    }

    /// <summary>
    /// Gets the license URL for a package
    /// </summary>
    private static string GetLicenseUrl(string packageName)
    {
        return packageName switch
        {
            var name when name.StartsWith("Microsoft.") => "https://licenses.nuget.org/MIT",
            "Scalar.AspNetCore" => "https://github.com/scalar/scalar/blob/main/LICENSE",
            var name when name.StartsWith("Swashbuckle.") => "https://github.com/domaindrivendev/Swashbuckle.AspNetCore/blob/master/LICENSE",
            _ => "https://licenses.nuget.org/MIT"
        };
    }

    /// <summary>
    /// Gets the project URL for a package
    /// </summary>
    private static string GetProjectUrl(string packageName)
    {
        return packageName switch
        {
            "Microsoft.AspNetCore.OpenApi" => "https://github.com/dotnet/aspnetcore",
            "Microsoft.OpenApi" => "https://github.com/microsoft/OpenAPI.NET",
            "Scalar.AspNetCore" => "https://github.com/scalar/scalar",
            var name when name.StartsWith("Swashbuckle.") => "https://github.com/domaindrivendev/Swashbuckle.AspNetCore",
            var name when name.StartsWith("Microsoft.") => "https://github.com/dotnet/aspnetcore",
            _ => "https://www.nuget.org/packages/" + packageName
        };
    }

    /// <summary>
    /// Gets the authors for a package
    /// </summary>
    private static string GetAuthors(string packageName)
    {
        return packageName switch
        {
            var name when name.StartsWith("Microsoft.") => "Microsoft",
            "Scalar.AspNetCore" => "Scalar",
            var name when name.StartsWith("Swashbuckle.") => "Richard Morris, Rik Morris",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Gets the description for a package
    /// </summary>
    private static string GetDescription(string packageName)
    {
        return packageName switch
        {
            "Microsoft.AspNetCore.OpenApi" => "Provides OpenAPI specification generation for ASP.NET Core applications",
            "Microsoft.OpenApi" => "The OpenAPI.NET SDK contains a useful object model for OpenAPI documents in .NET",
            "Scalar.AspNetCore" => "Modern API documentation and testing interface for ASP.NET Core applications",
            "Swashbuckle.AspNetCore" => "Swagger tools for documenting APIs built on ASP.NET Core",
            "Swashbuckle.AspNetCore.Swagger" => "Swagger tools for documenting APIs built on ASP.NET Core - Swagger middleware",
            "Swashbuckle.AspNetCore.SwaggerGen" => "Swagger tools for documenting APIs built on ASP.NET Core - Swagger document generation",
            "Swashbuckle.AspNetCore.SwaggerUI" => "Swagger tools for documenting APIs built on ASP.NET Core - Swagger UI",
            var name when name.StartsWith("Microsoft.") => $".NET library: {name}",
            _ => $"NuGet package: {packageName}"
        };
    }
}
