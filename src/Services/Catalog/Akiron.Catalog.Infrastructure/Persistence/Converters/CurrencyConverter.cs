using Akiron.Catalog.Domain.Pricing;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Akiron.Catalog.Infrastructure.Persistence.Converters;

/// <summary>
/// Stores <see cref="Currency"/> as its three-letter name rather than its integer value.
/// </summary>
/// <remarks>
/// Costs three bytes a row and buys a readable table: <c>SELECT</c> in psql shows TRY,
/// not 1. Same reasoning as the snake_case naming decision.
/// </remarks>
public sealed class CurrencyConverter() : ValueConverter<Currency, string>(
    currency => currency.ToString(),
    value => Enum.Parse<Currency>(value));
