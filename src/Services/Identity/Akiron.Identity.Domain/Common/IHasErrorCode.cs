namespace Akiron.Identity.Domain.Common;

/// <summary>
/// An exception that names its failure in a way a client can act on (ADR-0018).
/// </summary>
/// <remarks>
/// A copy of Catalog's interface, kept deliberately: this is the second service to need
/// it, which is the evidence slice 2.2 uses to decide what to extract.
/// </remarks>
public interface IHasErrorCode
{
    /// <summary>A stable identifier from <see cref="IdentityErrorCodes"/>.</summary>
    string Code { get; }

    /// <summary>Values the client needs to render its own message.</summary>
    IReadOnlyDictionary<string, object?> Parameters { get; }
}
