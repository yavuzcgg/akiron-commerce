namespace Akiron.Catalog.Domain.Common;

/// <summary>
/// A domain invariant was violated. Deliberately one type rather than one per rule:
/// the API maps every domain violation the same way, so distinct types would only
/// pay off if some caller needed to branch on them. Split when that day comes.
/// </summary>
public sealed class DomainValidationException(string message) : DomainException(message);
