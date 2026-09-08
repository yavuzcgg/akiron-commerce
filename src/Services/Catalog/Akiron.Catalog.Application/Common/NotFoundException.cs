namespace Akiron.Catalog.Application.Common;

/// <summary>The addressed resource does not exist. Mapped to 404.</summary>
public sealed class NotFoundException(string message) : Exception(message);
