using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RR.Core.Interfaces;
using RR.Core.Services;
using RR.DTO;
using RR.Http.Services;

namespace RR.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration testing
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program> {
    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.ConfigureServices(services => {
            // Remove all registrations of IPackageInfoService and replace with a test implementation
            var descriptors = services.Where(d => d.ServiceType == typeof(IPackageInfoService)).ToList();
            foreach (var descriptor in descriptors) {
                services.Remove(descriptor);
            }

            // Add test implementation of PackageInfoService
            services.AddSingleton<IPackageInfoService, TestPackageInfoService>();

            // Override authentication with a test scheme
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", null);

            // Add authorization services for tests
            services.AddAuthorization();

            // Configure test-specific file storage paths
            services.AddSingleton<IConfiguration>(sp => {
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?> {
                        ["FileStorage:DatabasePath"] = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
                        ["FileStorage:FilesPath"] = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
                    })
                    .Build();
                return config;
            });
        });

        builder.UseEnvironment("Testing");
    }

    protected override IHost CreateHost(IHostBuilder builder) {
        var host = base.CreateHost(builder);

        // Initialize test data
        using (var scope = host.Services.CreateScope()) {
            var dataInitService = scope.ServiceProvider.GetRequiredService<DataInitializationService>();
            dataInitService.InitializeTestData();
        }

        return host;
    }
}

/// <summary>
/// Test implementation of PackageInfoService for testing
/// </summary>
public class TestPackageInfoService : IPackageInfoService {
    public async Task<List<PackageInfo>> GetPackageInfoAsync() {
        await Task.Delay(1); // Simulate async operation
        return new List<PackageInfo>
        {
            new("TestPackage", "1.0.0", "MIT", "https://test.com/license", "https://test.com", "Test Author", "Test package for testing"),
            new("AnotherTestPackage", "2.0.0", "Apache-2.0", "https://apache.org/licenses/LICENSE-2.0", "https://example.com", "Another Author", "Another test package")
        };
    }
}
