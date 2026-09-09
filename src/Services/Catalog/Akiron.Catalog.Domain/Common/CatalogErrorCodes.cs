namespace Akiron.Catalog.Domain.Common;

/// <summary>
/// Every failure this service can report, named once.
/// </summary>
/// <remarks>
/// These strings are part of the public API contract (ADR-0018): clients branch on
/// them and translate them, so renaming one is a breaking change. The catalogue in
/// docs/API_CONVENTIONS.md is generated from this list by hand — keep the two in step.
/// Naming is service.resource.reason.
/// </remarks>
public static class CatalogErrorCodes
{
    // Domain guards. FluentValidation normally rejects bad input before the domain sees
    // it, so these mostly fire when something bypassed the endpoint — a developer signal
    // more than an end-user one.
    public const string TextRequired = "catalog.text.required";
    public const string TextTooLong = "catalog.text.too_long";
    public const string SlugInvalidFormat = "catalog.slug.invalid_format";
    public const string SlugTooLong = "catalog.slug.too_long";
    public const string SkuInvalidFormat = "catalog.sku.invalid_format";
    public const string SkuInvalidLength = "catalog.sku.invalid_length";
    public const string MoneyNegativeAmount = "catalog.money.negative_amount";
    public const string MoneyTooManyDecimals = "catalog.money.too_many_decimals";
    public const string MoneyUnsupportedCurrency = "catalog.money.unsupported_currency";
    public const string PriceGroupCodeInvalidFormat = "catalog.price_group.code_invalid_format";
    public const string DiscountOutOfRange = "catalog.discount.out_of_range";
    public const string PriceCurrencyMismatch = "catalog.price.currency_mismatch";
    public const string MarkupOutOfRange = "catalog.markup.out_of_range";
    public const string MarkupChainTooDeep = "catalog.markup_chain.too_deep";

    // Outcomes a caller can act on.
    public const string CategoryNotFound = "catalog.category.not_found";
    public const string CategorySlugConflict = "catalog.category.slug_conflict";
    public const string CategoryHasProducts = "catalog.category.has_products";
    public const string ProductNotFound = "catalog.product.not_found";
    public const string ProductSkuConflict = "catalog.product.sku_conflict";
    public const string PriceGroupNotFound = "catalog.price_group.not_found";
    public const string PriceGroupCodeConflict = "catalog.price_group.code_conflict";
    public const string PriceGroupHasPrices = "catalog.price_group.has_prices";
    public const string PriceListEntryNotFound = "catalog.price_list.entry_not_found";

    // Paging and sorting.
    public const string PageOutOfRange = "catalog.paging.page_out_of_range";
    public const string PageSizeTooLarge = "catalog.paging.page_size_too_large";
    public const string SortNotSupported = "catalog.paging.sort_not_supported";

    // Cross-cutting.
    public const string ValidationFailed = "validation_failed";
    public const string Conflict = "catalog.conflict";
    public const string Unexpected = "unexpected_error";
}
