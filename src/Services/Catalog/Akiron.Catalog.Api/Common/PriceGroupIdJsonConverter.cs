using System.Text.Json;
using System.Text.Json.Serialization;
using Akiron.Catalog.Domain.Pricing;

namespace Akiron.Catalog.Api.Common;

/// <summary>
/// Writes <see cref="PriceGroupId"/> as a bare uuid string.
/// </summary>
/// <remarks>
/// The third of these. Forgetting it is exactly what happened during 1.4: the price
/// group came back with its id wrapped in an object while every other id was a string.
/// The fourth typed id is the point at which a converter factory stops being premature.
/// </remarks>
public sealed class PriceGroupIdJsonConverter : JsonConverter<PriceGroupId>
{
    public override PriceGroupId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetGuid());

    public override void Write(Utf8JsonWriter writer, PriceGroupId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
