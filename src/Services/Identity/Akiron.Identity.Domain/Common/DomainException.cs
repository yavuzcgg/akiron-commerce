namespace Akiron.Identity.Domain.Common;

/// <summary>
/// Base for rule violations raised inside the domain. The API maps this family to 400.
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

/// <summary>A domain invariant was violated; the <see cref="DomainException.Code"/> says which.</summary>
public sealed class DomainValidationException(
    string code,
    string message,
    IReadOnlyDictionary<string, object?>? parameters = null)
    : DomainException(code, message, parameters);
