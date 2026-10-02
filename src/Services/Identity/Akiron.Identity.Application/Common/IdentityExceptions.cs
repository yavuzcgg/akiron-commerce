using Akiron.Identity.Domain.Common;

namespace Akiron.Identity.Application.Common;

/// <summary>Shared shape of the outcome exceptions below.</summary>
public abstract class OutcomeException(
    string code,
    string message,
    IReadOnlyDictionary<string, object?>? parameters) : Exception(message), IHasErrorCode
{
    private static readonly IReadOnlyDictionary<string, object?> NoParameters =
        new Dictionary<string, object?>();

    public string Code { get; } = code;

    public IReadOnlyDictionary<string, object?> Parameters { get; } = parameters ?? NoParameters;
}

/// <summary>The addressed resource does not exist. Mapped to 404.</summary>
public sealed class NotFoundException(
    string code, string message, IReadOnlyDictionary<string, object?>? parameters = null)
    : OutcomeException(code, message, parameters);

/// <summary>The request collides with the current state. Mapped to 409.</summary>
public sealed class ConflictException(
    string code, string message, IReadOnlyDictionary<string, object?>? parameters = null)
    : OutcomeException(code, message, parameters);

/// <summary>
/// The credentials were not accepted. Mapped to 401, and deliberately silent about why.
/// </summary>
public sealed class InvalidCredentialsException(string code, string message)
    : OutcomeException(code, message, parameters: null);
