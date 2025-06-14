using System.ComponentModel;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RR.Http.Configuration;

/// <summary>
/// Schema filter to include Description attributes in OpenAPI documentation
/// </summary>
public class DescriptionSchemaFilter : ISchemaFilter {
    public void Apply(OpenApiSchema schema, SchemaFilterContext context) {
        if (context.Type == null)
            return;

        // Handle properties on types
        var properties = context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties) {
            var descriptionAttribute = property.GetCustomAttribute<DescriptionAttribute>();
            if (descriptionAttribute != null && schema.Properties != null) {
                var propertyName = GetPropertyName(property, context);
                if (schema.Properties.TryGetValue(propertyName, out var propertySchema)) {
                    propertySchema.Description = descriptionAttribute.Description;
                }
            }
        }

        // Handle parameters on record types
        if (context.Type.IsClass && context.Type.GetConstructors().Length > 0) {
            var constructor = context.Type.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (constructor != null) {
                var parameters = constructor.GetParameters();
                foreach (var parameter in parameters) {
                    var descriptionAttribute = parameter.GetCustomAttribute<DescriptionAttribute>();
                    if (descriptionAttribute != null && schema.Properties != null) {
                        var propertyName = GetParameterPropertyName(parameter);
                        if (schema.Properties.TryGetValue(propertyName, out var propertySchema)) {
                            propertySchema.Description = descriptionAttribute.Description;
                        }
                    }
                }
            }
        }
    }

    private static string GetPropertyName(PropertyInfo property, SchemaFilterContext context) {
        // This tries to match the JSON property naming convention
        var propertyName = property.Name;

        // Convert to camelCase if that's the naming policy
        return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }

    private static string GetParameterPropertyName(ParameterInfo parameter) {
        // Convert parameter name to camelCase property name
        var parameterName = parameter.Name ?? "";
        return char.ToLowerInvariant(parameterName[0]) + parameterName[1..];
    }
}
