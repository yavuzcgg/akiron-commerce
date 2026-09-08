namespace Akiron.Catalog.Application.Common;

/// <summary>
/// The request collides with the current state — a duplicate slug, say. Mapped to 409.
/// </summary>
public sealed class ConflictException(string message) : Exception(message);
