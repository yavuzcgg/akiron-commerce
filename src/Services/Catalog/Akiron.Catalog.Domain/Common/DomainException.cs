namespace Akiron.Catalog.Domain.Common;

/// <summary>
/// Base for rule violations raised inside the domain: a value object rejecting its
/// input, or an entity refusing a state change. The API maps this family to 400 —
/// it always means the caller sent something the domain cannot represent.
/// </summary>
public abstract class DomainException(
    string code,
    string message,
    IReadOnlyDictionary<string, object?>? parameters = null) : Exception(message), IHasErrorCode
{
    private static readonly IReadOnlyDictionary<string, object?> NoParameters =
        new Dictionary<string, object?>();

    public string Code { get; } = code;

    public IReadOnlyDictionary<string, object?> Parameters { get; } = parameters ?? NoParameters;
}
