using Akiron.Catalog.Domain.Common;

namespace Akiron.Catalog.Application.Common;

/// <summary>The addressed resource does not exist. Mapped to 404.</summary>
public sealed class NotFoundException(
    string code,
    string message,
    IReadOnlyDictionary<string, object?>? parameters = null) : Exception(message), IHasErrorCode
{
    private static readonly IReadOnlyDictionary<string, object?> NoParameters =
        new Dictionary<string, object?>();

    public string Code { get; } = code;

    public IReadOnlyDictionary<string, object?> Parameters { get; } = parameters ?? NoParameters;
}
