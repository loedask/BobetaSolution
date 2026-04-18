using Bobeta.API.App.Filters;
using Microsoft.OpenApi.Models;

namespace Bobeta.API.App.Extensions;

/// <summary>
/// Centralizes Swagger/OpenAPI registration and pipeline setup for the Bobeta API.
/// </summary>
public static class SwaggerExtensions
{
    private const string DocName = "v1";
    private const string DocTitle = "Bobeta API";
    /// <summary>OpenAPI <c>info.version</c> (API release); keep a semantic-style string for strict Swagger UI / tooling.</summary>
    private const string OpenApiApiVersion = "1.0.0";

    /// <summary>
    /// Adds Swagger Gen with Bearer security and optional XML comments.
    /// </summary>
    public static IServiceCollection AddBobetaSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(DocName, new OpenApiInfo { Title = DocTitle, Version = OpenApiApiVersion });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                In = ParameterLocation.Header,
                Description = "JWT Bearer token for Bobeta API."
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);

            options.SchemaFilter<DomainEnumVarNamesSchemaFilter>();
        });
        return services;
    }

    /// <summary>
    /// Enables Swagger and Swagger UI, and redirects root URL to /swagger.
    /// </summary>
    public static WebApplication UseBobetaSwagger(this WebApplication app)
    {
        app.UseSwagger();
        // Relative URL: avoids some reverse-proxy / base-path issues when fetching the spec (absolute "/swagger/..." can mis-resolve on Azure).
        app.UseSwaggerUI(options => options.SwaggerEndpoint($"{DocName}/swagger.json", $"{DocTitle} {DocName}"));
        app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
        return app;
    }
}
