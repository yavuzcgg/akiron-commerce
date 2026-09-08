using Akiron.Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Akiron.Catalog.Infrastructure.Persistence.Converters;

/// <summary>Stores <see cref="CategoryId"/> as the plain uuid it wraps.</summary>
public sealed class CategoryIdConverter() : ValueConverter<CategoryId, Guid>(
    id => id.Value,
    value => new CategoryId(value));
