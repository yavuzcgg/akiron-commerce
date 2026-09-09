namespace Akiron.Catalog.Domain.Pricing;

/// <summary>
/// The currencies the catalogue can price in.
/// </summary>
/// <remarks>
/// Numbering starts at 1 deliberately: default(Currency) is then 0, which is not a
/// defined member, so a Money that never went through its factory fails validation
/// instead of silently claiming to be Turkish lira.
/// </remarks>
public enum Currency
{
    TRY = 1,
    USD = 2,
    EUR = 3,
}
