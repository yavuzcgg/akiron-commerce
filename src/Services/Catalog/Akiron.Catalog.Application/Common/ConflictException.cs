using Akiron.Catalog.Domain.Common;

namespace Akiron.Catalog.Application.Common;

/// <summary>
/// The request collides with the current state — a duplicate slug, say. Mapped to 409.
/// </summary>
public sealed class ConflictException(
    string code,
    string message,
    IReadOnlyDictionary<string, object?>? parameters = null) : Exception(message), IHasErrorCode
{
    private static readonly IReadOnlyDictionary<string, object?> NoParameters =
        new Dictionary<string, object?>();

    public string Code { get; } = code;

    public IReadOnlyDictionary<string, object?> Parameters { get; } = parameters ?? NoParameters;
}
