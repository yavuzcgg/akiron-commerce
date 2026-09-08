using Akiron.Catalog.Domain.Categories;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Akiron.Catalog.Api.Common;

/// <summary>
/// Describes typed ids as plain uuid strings in the OpenAPI document.
/// </summary>
/// <remarks>
/// The generator reflects over the CLR type and would otherwise document
/// <c>CategoryId</c> as an object with a <c>value</c> property — which is not what
/// the JSON converter actually writes.
/// </remarks>
public sealed class TypedIdSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type == typeof(CategoryId))
        {
            schema.Type = JsonSchemaType.String;
            schema.Format = "uuid";
            schema.Properties?.Clear();
        }

        return Task.CompletedTask;
    }
}
