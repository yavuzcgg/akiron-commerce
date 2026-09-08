using System.Text.Json;
using System.Text.Json.Serialization;
using Akiron.Catalog.Domain.Categories;

namespace Akiron.Catalog.Api.Common;

/// <summary>
/// Writes <see cref="CategoryId"/> as a bare uuid string.
/// </summary>
/// <remarks>
/// Without this the default serializer would render the wrapper as
/// <c>{"value":"…"}</c> and leak an implementation detail into the public contract.
/// The converter lives here, not on the domain type, so the domain keeps no opinion
/// about JSON.
/// </remarks>
public sealed class CategoryIdJsonConverter : JsonConverter<CategoryId>
{
    public override CategoryId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetGuid());

    public override void Write(Utf8JsonWriter writer, CategoryId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
