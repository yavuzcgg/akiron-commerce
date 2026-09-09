using Akiron.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Akiron.Catalog.Infrastructure.Persistence.Converters;

/// <summary>Stores <see cref="ProductId"/> as the plain uuid it wraps.</summary>
public sealed class ProductIdConverter() : ValueConverter<ProductId, Guid>(
    id => id.Value,
    value => new ProductId(value));
