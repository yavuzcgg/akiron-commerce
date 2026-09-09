using Akiron.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Akiron.Catalog.Infrastructure.Persistence.Converters;

/// <summary>
/// Stores <see cref="Sku"/> as text, re-validating on the way back so a row edited by
/// hand into an invalid SKU fails loudly instead of spreading.
/// </summary>
public sealed class SkuConverter() : ValueConverter<Sku, string>(
    sku => sku.Value,
    value => Sku.Create(value));
