using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RR.DTO;
using RR.Http.Services;

namespace RR.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration testing
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program> {
    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.ConfigureServices(services => {
            // Remove the real PackageInfoService and replace with a test implementation
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(PackageInfoService));

            if (descriptor != null) {
                services.Remove(descriptor);
            }

            // Add test implementation of PackageInfoService
            services.AddSingleton<PackageInfoService, TestPackageInfoService>();
        });

        builder.UseEnvironment("Testing");
    }
}

/// <summary>
/// Test implementation of PackageInfoService for testing
/// </summary>
public class TestPackageInfoService : PackageInfoService {
    public TestPackageInfoService() : base(
        Microsoft.Extensions.Logging.Abstractions.NullLogger<PackageInfoService>.Instance,
        new HttpClient()) {
    }

    public override async Task<List<PackageInfo>> GetPackageInfoAsync() {
        await Task.Delay(1); // Simulate async operation
        return new List<PackageInfo>
        {
            new("TestPackage", "1.0.0", "MIT", "https://test.com/license", "https://test.com", "Test Author", "Test package for testing"),
            new("AnotherTestPackage", "2.0.0", "Apache-2.0", "https://apache.org/licenses/LICENSE-2.0", "https://example.com", "Another Author", "Another test package")
        };
    }
}
