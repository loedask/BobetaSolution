using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Bobeta.API.App.Filters;

/// <summary>
/// Adds <c>x-enum-varnames</c> for Bobeta.Domain enums so NSwag generates readable C# enum members
/// instead of <c>_0</c>, <c>_1</c>, … when the schema uses integer <c>enum</c>.
/// </summary>
public sealed class DomainEnumVarNamesSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsEnum)
            return;

        if (context.Type.Namespace is null || !context.Type.Namespace.StartsWith("Bobeta.Domain", StringComparison.Ordinal))
            return;

        if (schema.Type != "integer" || schema.Enum is null || schema.Enum.Count == 0)
            return;

        var namesInValueOrder = Enum.GetValues(context.Type)
            .Cast<object>()
            .OrderBy(Convert.ToInt32)
            .Select(v => Enum.GetName(context.Type, v)!)
            .ToArray();

        var arr = new OpenApiArray();
        foreach (var name in namesInValueOrder)
            arr.Add(new OpenApiString(name));

        schema.Extensions["x-enum-varnames"] = arr;
    }
}
