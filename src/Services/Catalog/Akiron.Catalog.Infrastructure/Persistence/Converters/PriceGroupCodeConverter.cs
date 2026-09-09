using Akiron.Catalog.Domain.Pricing;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Akiron.Catalog.Infrastructure.Persistence.Converters;

/// <summary>
/// Stores <see cref="PriceGroupCode"/> as text, re-validating on the way back so a row
/// edited by hand into an invalid code fails loudly instead of spreading.
/// </summary>
public sealed class PriceGroupCodeConverter() : ValueConverter<PriceGroupCode, string>(
    code => code.Value,
    value => PriceGroupCode.Create(value));
