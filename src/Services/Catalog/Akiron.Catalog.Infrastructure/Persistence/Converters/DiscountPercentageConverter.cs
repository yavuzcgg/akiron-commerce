using Akiron.Catalog.Domain.Pricing;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Akiron.Catalog.Infrastructure.Persistence.Converters;

/// <summary>Stores <see cref="DiscountPercentage"/> as the decimal it wraps.</summary>
public sealed class DiscountPercentageConverter() : ValueConverter<DiscountPercentage, decimal>(
    discount => discount.Value,
    value => DiscountPercentage.Create(value));
