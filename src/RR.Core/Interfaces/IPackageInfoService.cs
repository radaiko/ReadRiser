using RR.DTO;

namespace RR.Core.Interfaces;

/// <summary>
/// Service for gathering information about NuGet packages used in the application
/// </summary>
public interface IPackageInfoService {
    /// <summary>
    /// Gets information about all packages used in the application
    /// </summary>
    /// <returns>List of package information including licenses</returns>
    Task<List<PackageInfo>> GetPackageInfoAsync();
}
