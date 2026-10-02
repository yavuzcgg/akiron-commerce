using Akiron.Identity.Domain.Users;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Akiron.Identity.Api.Common;

/// <summary>Describes typed ids as plain uuid strings in the OpenAPI document, matching the JSON converters.</summary>
public sealed class TypedIdSchemaTransformer : IOpenApiSchemaTransformer
{
    private static readonly HashSet<Type> TypedIds = [typeof(UserId)];

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
