namespace Akiron.Catalog.Domain.Common;

/// <summary>
/// A domain invariant was violated. Deliberately one type rather than one per rule:
/// the API maps every domain violation the same way, and what distinguishes them for a
/// client is the <see cref="DomainException.Code"/>, not the CLR type.
/// </summary>
public sealed class DomainValidationException(
    string code,
    string message,
    IReadOnlyDictionary<string, object?>? parameters = null)
    : DomainException(code, message, parameters);
