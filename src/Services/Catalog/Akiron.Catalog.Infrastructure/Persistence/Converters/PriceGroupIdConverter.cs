using Akiron.Catalog.Domain.Pricing;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Akiron.Catalog.Infrastructure.Persistence.Converters;

/// <summary>Stores <see cref="PriceGroupId"/> as the plain uuid it wraps.</summary>
public sealed class PriceGroupIdConverter() : ValueConverter<PriceGroupId, Guid>(
    id => id.Value,
    value => new PriceGroupId(value));
