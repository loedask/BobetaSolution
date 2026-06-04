using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Bobeta.API.App.Filters;

/// <summary>
/// Adds <c>x-enum-varnames</c> for Bobeta.Domain enums so OpenAPI consumers get readable enum member names.
/// instead of <c>_0</c>, <c>_1</c>, ... when the schema uses integer <c>enum</c>.
/// </summary>
public sealed class DomainEnumVarNamesSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        try
        {
            if (schema is not OpenApiSchema openApiSchema)
                return;

            if (!context.Type.IsEnum)
                return;

            if (context.Type.Namespace is null || !context.Type.Namespace.StartsWith("Bobeta.Domain", StringComparison.Ordinal))
                return;

            if (openApiSchema.Type != JsonSchemaType.Integer || openApiSchema.Enum is null || openApiSchema.Enum.Count == 0)
                return;

            var namesInValueOrder = Enum.GetValues(context.Type)
                .Cast<object>()
                .OrderBy(static v => Convert.ToInt64(v))
                .Select(v => Enum.GetName(context.Type, v)!)
                .ToArray();

            var arr = new JsonArray();
            foreach (var name in namesInValueOrder)
                arr.Add(name);

            if (openApiSchema.Extensions is null)
                return;

            openApiSchema.Extensions["x-enum-varnames"] = new JsonNodeExtension(arr);
        }
        catch
        {
            // Avoid failing OpenAPI generation (Swagger UI would return 500 for /swagger/v1/swagger.json).
        }
    }
}
