using Akiron.Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Akiron.Catalog.Infrastructure.Persistence.Converters;

/// <summary>
/// Stores <see cref="Slug"/> as text.
/// </summary>
/// <remarks>
/// Reading goes through <see cref="Slug.Create"/>, so a row hand-edited into an
/// invalid slug fails loudly instead of spreading. That re-runs the format check on
/// every materialised row; it is a regex over a short string, and correctness is
/// worth more here than the microseconds. Revisit only with a profile in hand.
/// </remarks>
public sealed class SlugConverter() : ValueConverter<Slug, string>(
    slug => slug.Value,
    value => Slug.Create(value));
