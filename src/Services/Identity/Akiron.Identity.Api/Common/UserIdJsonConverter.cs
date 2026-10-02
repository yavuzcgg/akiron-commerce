using System.Text.Json;
using System.Text.Json.Serialization;
using Akiron.Identity.Domain.Users;

namespace Akiron.Identity.Api.Common;

/// <summary>Writes <see cref="UserId"/> as a bare uuid string, never as <c>{"value": …}</c>.</summary>
public sealed class UserIdJsonConverter : JsonConverter<UserId>
{
    public override UserId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(reader.GetGuid());

    public override void Write(Utf8JsonWriter writer, UserId value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.Value);
}
