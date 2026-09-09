using Akiron.Catalog.Domain.Categories;
using Akiron.Catalog.Domain.Products;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Akiron.Catalog.Api.Common;

/// <summary>
/// Describes typed ids as plain uuid strings in the OpenAPI document.
/// </summary>
/// <remarks>
/// The generator reflects over the CLR type and would otherwise document CategoryId as
/// an object with a <c>value</c> property, which is not what the JSON converters write.
/// The set below is the one place a new typed id has to be registered.
/// </remarks>
public sealed class TypedIdSchemaTransformer : IOpenApiSchemaTransformer
{
    private static readonly HashSet<Type> TypedIds = [typeof(CategoryId), typeof(ProductId)];

    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (TypedIds.Contains(context.JsonTypeInfo.Type))
        {
            schema.Type = JsonSchemaType.String;
            schema.Format = "uuid";
            schema.Properties?.Clear();
        }

        return Task.CompletedTask;
    }
}
