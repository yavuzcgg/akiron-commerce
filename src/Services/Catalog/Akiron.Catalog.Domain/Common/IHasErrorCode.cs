namespace Akiron.Catalog.Domain.Common;

/// <summary>
/// An exception that names its failure in a way a client can act on.
/// </summary>
/// <remarks>
/// The API writes <see cref="Code"/> and <see cref="Parameters"/> into the problem
/// response so callers can translate it (ADR-0018). The exception message stays
/// English and is for logs and developers; it is never the text a shopper reads.
/// </remarks>
public interface IHasErrorCode
{
    /// <summary>A stable identifier from <see cref="CatalogErrorCodes"/>.</summary>
    string Code { get; }

    /// <summary>Values the client needs to render its own message, e.g. the SKU that clashed.</summary>
    IReadOnlyDictionary<string, object?> Parameters { get; }
}
