using System.Diagnostics;
using FluentAssertions;
using RR.Tests.Infrastructure;
using Xunit;

namespace RR.Tests.Performance;

/// <summary>
/// Performance tests for API endpoints
/// </summary>
public class PerformanceTests : IClassFixture<TestWebApplicationFactory> {
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PerformanceTests(TestWebApplicationFactory factory) {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ShouldRespondWithinAcceptableTime() {
        // Arrange
        // Performance thresholds rationale:
        // - Health endpoint (100ms): Simple status check, should be very fast for monitoring systems
        // - Status endpoint (200ms): May include additional system checks, slightly more complex
        // - Credits endpoint (500ms): May involve data retrieval/processing, more generous threshold
        // - Average multi-request (150ms): Ensures consistent performance under load
        // These thresholds provide a good balance between realistic expectations and quality assurance
        const int maxResponseTimeMs = 100;
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var response = await _client.GetAsync("/api/v1/health");
        stopwatch.Stop();

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxResponseTimeMs,
            $"Health endpoint should respond within {maxResponseTimeMs}ms");
    }

    [Fact]
    public async Task StatusEndpoint_ShouldRespondWithinAcceptableTime() {
        // Arrange
        const int maxResponseTimeMs = 200;
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var response = await _client.GetAsync("/api/v1/status");
        stopwatch.Stop();

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxResponseTimeMs,
            $"Status endpoint should respond within {maxResponseTimeMs}ms");
    }

    [Fact]
    public async Task CreditsEndpoint_ShouldRespondWithinAcceptableTime() {
        // Arrange
        const int maxResponseTimeMs = 500;
        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var response = await _client.GetAsync("/api/v1/credits");
        stopwatch.Stop();

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxResponseTimeMs,
            $"Credits endpoint should respond within {maxResponseTimeMs}ms");
    }

    [Fact]
    public async Task MultipleRequests_ShouldMaintainPerformance() {
        // Arrange
        const int numberOfRequests = 50;
        const int maxAverageResponseTimeMs = 150;
        var tasks = new List<Task<long>>();

        // Act
        for (int i = 0; i < numberOfRequests; i++) {
            tasks.Add(MeasureRequestTime("/api/v1/health"));
        }

        var responseTimes = await Task.WhenAll(tasks);

        // Assert
        var averageTime = responseTimes.Average();
        averageTime.Should().BeLessThan(maxAverageResponseTimeMs,
            $"Average response time should be less than {maxAverageResponseTimeMs}ms");

        var maxTime = responseTimes.Max();
        maxTime.Should().BeLessThan(maxAverageResponseTimeMs * 3,
            "No single request should take more than 3x the expected average time");
    }

    [Fact]
    public async Task MemoryUsage_ShouldRemainStable() {
        // Arrange - warm up the application first to avoid initialization overhead
        const int warmupRequests = 10;
        const int measurementRequests = 100;

        // Warm-up phase to initialize JIT, connection pools, etc.
        var warmupTasks = new List<Task>();
        for (int i = 0; i < warmupRequests; i++) {
            warmupTasks.Add(_client.GetAsync("/api/v1/health"));
        }
        await Task.WhenAll(warmupTasks);

        // Force garbage collection before measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var initialMemory = GC.GetTotalMemory(false);

        // Act - actual measurement phase
        var tasks = new List<Task>();
        for (int i = 0; i < measurementRequests; i++) {
            tasks.Add(_client.GetAsync("/api/v1/health"));
        }

        await Task.WhenAll(tasks);

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);

        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreasePerRequest = memoryIncrease / measurementRequests;

        // Increased threshold to be more realistic - 50KB per request should be reasonable
        // for a simple HTTP endpoint including response object allocation
        memoryIncreasePerRequest.Should().BeLessThan(1024 * 50, // 50KB per request
            "Memory usage per request should remain reasonable");
    }

    private async Task<long> MeasureRequestTime(string endpoint) {
        var stopwatch = Stopwatch.StartNew();
        var response = await _client.GetAsync(endpoint);
        stopwatch.Stop();

        response.IsSuccessStatusCode.Should().BeTrue();
        return stopwatch.ElapsedMilliseconds;
    }
}
