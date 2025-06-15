using System.Reflection;
using Microsoft.OpenApi.Models;
using RR.Http.Configuration;
using RR.Http.Endpoints;
using RR.Http.Services;
using RRHttp.Endpoints;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add authentication and authorization services
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// Add Swagger services for better schema generation
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo {
        Title = "ReadRiser API",
        Version = "v1",
        Description = "API for ReadRiser application"
    });

    // Add the description schema filter
    c.SchemaFilter<DescriptionSchemaFilter>();

    // Include XML comments if available
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) {
        c.IncludeXmlComments(xmlPath);
    }

    // Also include XML comments from referenced assemblies
    var dtoXmlFile = "RR.DTO.xml";
    var dtoXmlPath = Path.Combine(AppContext.BaseDirectory, dtoXmlFile);
    if (File.Exists(dtoXmlPath)) {
        c.IncludeXmlComments(dtoXmlPath);
    }
});

// Add HTTP client for external API calls
builder.Services.AddHttpClient();

// Add custom services
builder.Services.AddScoped<PackageInfoService>();
builder.Services.AddSingleton<FileBasedDbService>();
builder.Services.AddSingleton<FileStorageService>();
builder.Services.AddScoped<UserManagementService>();
builder.Services.AddScoped<FileManagementService>();
builder.Services.AddScoped<DataInitializationService>();

// Add API explorer services for endpoint discovery
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.MapOpenApi();
    app.MapScalarApiReference(options => {
        options.Title = "ReadRiser API Documentation";
        options.Theme = ScalarTheme.Purple;
        options.ShowSidebar = true;
        options.OpenApiRoutePattern = "/swagger/v1/swagger.json";
    });
}

app.UseHttpsRedirection();

// Add authentication middleware
app.UseAuthentication();
app.UseAuthorization();

// Initialize test data
using (var scope = app.Services.CreateScope()) {
    var dataInitService = scope.ServiceProvider.GetRequiredService<DataInitializationService>();
    dataInitService.InitializeTestData();
}

// Map endpoints
app.MapBasicEndpoints();
app.MapUserEndpoints();
app.MapFileEndpoints();

app.Run();

// Make Program class accessible for testing
public partial class Program { }
