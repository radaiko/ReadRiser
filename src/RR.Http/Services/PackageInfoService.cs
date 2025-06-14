using System.Text.Json;
using System.Text.Json.Nodes;
using RR.DTO;

namespace RR.Http.Services;

/// <summary>
/// Service for gathering information about NuGet packages used in the application
/// </summary>
public class PackageInfoService {
    private readonly ILogger<PackageInfoService> _logger;
    private readonly HttpClient _httpClient;

    public PackageInfoService(ILogger<PackageInfoService> logger, HttpClient httpClient) {
        _logger = logger;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Gets information about all packages used in the application
    /// </summary>
    /// <returns>List of package information including licenses</returns>
    public async Task<List<PackageInfo>> GetPackageInfoAsync() {
        var packages = await GetPackagesFromProjectAssetsAsync();
        _logger.LogInformation("Retrieved information for {PackageCount} packages from project.assets.json", packages.Count);
        return packages.OrderBy(p => p.Name).ToList();
    }

    /// <summary>
    /// Gets packages from project.assets.json file automatically
    /// </summary>
    private async Task<List<PackageInfo>> GetPackagesFromProjectAssetsAsync() {
        var packages = new List<PackageInfo>();

        try {
            // Find the project.assets.json file
            var currentDirectory = Directory.GetCurrentDirectory();
            var projectAssetsPath = Path.Combine(currentDirectory, "obj", "project.assets.json");

            if (!File.Exists(projectAssetsPath)) {
                _logger.LogWarning("project.assets.json not found at {Path}", projectAssetsPath);
                return packages;
            }

            var jsonContent = await File.ReadAllTextAsync(projectAssetsPath);
            var jsonDocument = JsonNode.Parse(jsonContent);

            if (jsonDocument == null) {
                _logger.LogWarning("Failed to parse project.assets.json");
                return packages;
            }

            // Get the libraries section which contains all package information
            var libraries = jsonDocument["libraries"]?.AsObject();
            if (libraries == null) {
                _logger.LogWarning("No libraries section found in project.assets.json");
                return packages;
            }

            // Get direct dependencies from the project section
            var directDependencies = GetDirectDependencies(jsonDocument);

            foreach (var library in libraries) {
                var packageName = library.Key;
                var packageInfo = library.Value?.AsObject();

                if (packageInfo == null)
                    continue;

                // Skip if not a package (could be project reference)
                var type = packageInfo["type"]?.GetValue<string>();
                if (type != "package")
                    continue;

                // Parse package name and version from the key (format: "PackageName/Version")
                var parts = packageName.Split('/');
                if (parts.Length != 2)
                    continue;

                var name = parts[0];
                var version = parts[1];

                // Only include direct dependencies or well-known packages
                var wellKnownPackages = new[]
                {
                    "Microsoft.AspNetCore.OpenApi",
                    "Microsoft.OpenApi",
                    "Scalar.AspNetCore",
                    "Swashbuckle.AspNetCore.Swagger",
                    "Swashbuckle.AspNetCore.SwaggerGen",
                    "Swashbuckle.AspNetCore.SwaggerUI"
                };

                if (!directDependencies.Contains(name) && !wellKnownPackages.Contains(name))
                    continue;

                // Fetch package metadata from NuGet.org
                var nugetMetadata = await GetNuGetPackageMetadataAsync(name, version);

                var packageInfoDto = new PackageInfo(
                    Name: name,
                    Version: version,
                    LicenseType: GetLicenseType(nugetMetadata),
                    LicenseUrl: nugetMetadata?.LicenseUrl ?? "Unknown",
                    ProjectUrl: nugetMetadata?.ProjectUrl ?? $"https://www.nuget.org/packages/{name}",
                    Authors: nugetMetadata?.Authors ?? "Unknown",
                    Description: nugetMetadata?.Description ?? nugetMetadata?.Title ?? $"NuGet package: {name}"
                );

                packages.Add(packageInfoDto);
            }

            _logger.LogInformation("Discovered {Count} packages from project.assets.json", packages.Count);
        } catch (Exception ex) {
            _logger.LogError(ex, "Error reading packages from project.assets.json");
        }

        return packages;
    }

    /// <summary>
    /// Gets direct dependencies from the project section
    /// </summary>
    private static HashSet<string> GetDirectDependencies(JsonNode jsonDocument) {
        var dependencies = new HashSet<string>();

        try {
            var frameworks = jsonDocument["project"]?["frameworks"]?.AsObject();
            if (frameworks != null) {
                foreach (var framework in frameworks) {
                    var deps = framework.Value?["dependencies"]?.AsObject();
                    if (deps != null) {
                        foreach (var dep in deps) {
                            dependencies.Add(dep.Key);
                        }
                    }
                }
            }
        } catch (Exception) {
            // Ignore errors in parsing dependencies
        }

        return dependencies;
    }

    /// <summary>
    /// Gets the license type for a package
    /// </summary>
    private static string GetLicenseType(NuGetPackageMetadata? metadata) {
        if (!string.IsNullOrEmpty(metadata?.LicenseExpression)) {
            return metadata.LicenseExpression;
        }

        if (!string.IsNullOrEmpty(metadata?.LicenseUrl)) {
            // Try to determine license type from URL
            var licenseUrl = metadata.LicenseUrl.ToLowerInvariant();
            if (licenseUrl.Contains("mit"))
                return "MIT";
            if (licenseUrl.Contains("apache"))
                return "Apache-2.0";
            if (licenseUrl.Contains("bsd"))
                return "BSD";
            if (licenseUrl.Contains("gpl"))
                return "GPL";
        }

        return "Unknown";
    }

    /// <summary>
    /// Fetches package metadata from NuGet.org API
    /// </summary>
    private async Task<NuGetPackageMetadata?> GetNuGetPackageMetadataAsync(string packageName, string version) {
        try {
            var url = $"https://api.nuget.org/v3-flatcontainer/{packageName.ToLowerInvariant()}/{version}/{packageName.ToLowerInvariant()}.nuspec";

            // First try to get the .nuspec file
            var nuspecResponse = await _httpClient.GetAsync(url);
            if (nuspecResponse.IsSuccessStatusCode) {
                var nuspecContent = await nuspecResponse.Content.ReadAsStringAsync();
                return ParseNuspecContent(nuspecContent);
            }

            // Fallback to registration API
            var registrationUrl = $"https://api.nuget.org/v3/registration5-gz-semver2/{packageName.ToLowerInvariant()}/{version}.json";
            var registrationResponse = await _httpClient.GetAsync(registrationUrl);

            if (registrationResponse.IsSuccessStatusCode) {
                var registrationContent = await registrationResponse.Content.ReadAsStringAsync();
                var registrationData = JsonSerializer.Deserialize<JsonNode>(registrationContent);

                return ParseRegistrationData(registrationData);
            }

            _logger.LogWarning("Could not fetch metadata for package {PackageName} version {Version}", packageName, version);
            return null;
        } catch (Exception ex) {
            _logger.LogError(ex, "Error fetching metadata for package {PackageName} version {Version}", packageName, version);
            return null;
        }
    }

    /// <summary>
    /// Parses nuspec XML content to extract metadata
    /// </summary>
    private static NuGetPackageMetadata? ParseNuspecContent(string nuspecContent) {
        try {
            // Simple XML parsing for nuspec content
            var lines = nuspecContent.Split('\n');

            string? id = null, version = null, title = null, description = null, authors = null, projectUrl = null, licenseUrl = null, licenseExpression = null;

            foreach (var line in lines) {
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("<id>"))
                    id = ExtractXmlValue(trimmedLine, "id");
                else if (trimmedLine.StartsWith("<version>"))
                    version = ExtractXmlValue(trimmedLine, "version");
                else if (trimmedLine.StartsWith("<title>"))
                    title = ExtractXmlValue(trimmedLine, "title");
                else if (trimmedLine.StartsWith("<description>"))
                    description = ExtractXmlValue(trimmedLine, "description");
                else if (trimmedLine.StartsWith("<authors>"))
                    authors = ExtractXmlValue(trimmedLine, "authors");
                else if (trimmedLine.StartsWith("<projectUrl>"))
                    projectUrl = ExtractXmlValue(trimmedLine, "projectUrl");
                else if (trimmedLine.StartsWith("<licenseUrl>"))
                    licenseUrl = ExtractXmlValue(trimmedLine, "licenseUrl");
                else if (trimmedLine.StartsWith("<license ") && trimmedLine.Contains("type=\"expression\"")) {
                    licenseExpression = ExtractXmlValue(trimmedLine, "license");
                }
            }

            return new NuGetPackageMetadata(id, version, title, description, authors, projectUrl, licenseUrl, licenseExpression, null);
        } catch {
            return null;
        }
    }

    /// <summary>
    /// Extracts value from simple XML element
    /// </summary>
    private static string? ExtractXmlValue(string xmlLine, string elementName) {
        var startTag = $"<{elementName}>";
        var endTag = $"</{elementName}>";

        var startIndex = xmlLine.IndexOf(startTag);
        var endIndex = xmlLine.IndexOf(endTag);

        if (startIndex >= 0 && endIndex > startIndex) {
            return xmlLine.Substring(startIndex + startTag.Length, endIndex - startIndex - startTag.Length).Trim();
        }

        return null;
    }

    /// <summary>
    /// Parses registration API JSON data
    /// </summary>
    private static NuGetPackageMetadata? ParseRegistrationData(JsonNode? registrationData) {
        try {
            var catalogEntry = registrationData?["catalogEntry"];
            if (catalogEntry == null)
                return null;

            var id = catalogEntry["id"]?.GetValue<string>();
            var version = catalogEntry["version"]?.GetValue<string>();
            var title = catalogEntry["title"]?.GetValue<string>();
            var description = catalogEntry["description"]?.GetValue<string>();
            var authors = catalogEntry["authors"]?.GetValue<string>();
            var projectUrl = catalogEntry["projectUrl"]?.GetValue<string>();
            var licenseUrl = catalogEntry["licenseUrl"]?.GetValue<string>();
            var licenseExpression = catalogEntry["licenseExpression"]?.GetValue<string>();

            return new NuGetPackageMetadata(id, version, title, description, authors, projectUrl, licenseUrl, licenseExpression, null);
        } catch {
            return null;
        }
    }
}
