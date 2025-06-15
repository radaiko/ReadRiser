using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using RR.Core.Interfaces;
using RR.Core.Services;

namespace RR.Core.Extensions;

/// <summary>
/// Extension methods for registering Core services with dependency injection
/// </summary>
public static class ServiceCollectionExtensions {
    /// <summary>
    /// Adds all Core business logic services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddCoreServices(this IServiceCollection services) {
        // Register database service
        services.AddSingleton<IFileBasedDbService, FileBasedDbService>();

        // Register storage service
        services.AddSingleton<IFileStorageService, FileStorageService>();

        // Register business logic services
        services.AddScoped<IUserManagementService, UserManagementService>();
        services.AddScoped<IFileManagementService, FileManagementService>();
        services.AddScoped<IPackageInfoService, PackageInfoService>();

        // Register HttpClient for PackageInfoService
        services.AddHttpClient<IPackageInfoService, PackageInfoService>();

        // Register data initialization service
        services.AddScoped<DataInitializationService>();

        return services;
    }
}
