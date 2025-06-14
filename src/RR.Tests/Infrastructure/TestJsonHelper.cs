using System.Text.Json;

namespace RR.Tests.Infrastructure;

/// <summary>
/// Provides common JSON serialization utilities for tests
/// </summary>
public static class TestJsonHelper {
    /// <summary>
    /// Standard JsonSerializerOptions used across all tests for consistency
    /// </summary>
    public static readonly JsonSerializerOptions DefaultOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Deserializes JSON content using the standard test options
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="content">The JSON content to deserialize</param>
    /// <returns>The deserialized object</returns>
    public static T? Deserialize<T>(string content) {
        return JsonSerializer.Deserialize<T>(content, DefaultOptions);
    }
}
