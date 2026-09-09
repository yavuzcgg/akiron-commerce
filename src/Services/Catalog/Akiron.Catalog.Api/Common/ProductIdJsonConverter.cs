using System.Text.Json;
using System.Text.Json.Serialization;
using Akiron.Catalog.Domain.Products;

namespace Akiron.Catalog.Api.Common;

/// <summary>
/// Writes <see cref="ProductId"/> as a bare uuid string.
/// </summary>
/// <remarks>
/// Deliberately a second explicit converter rather than a generic factory. Each is
/// eight obvious lines and is registered visibly in Program.cs; a factory only starts
/// paying for itself around the fourth typed id, which is when to revisit this.
/// </remarks>
public sealed class ProductIdJsonConverter : JsonConverter<ProductId>
{
    public override ProductId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetGuid());

    public override void Write(Utf8JsonWriter writer, ProductId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
