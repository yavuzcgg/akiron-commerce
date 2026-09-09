using Akiron.Catalog.Domain.Pricing;
using Akiron.Catalog.Domain.Products;
using FluentValidation;

namespace Akiron.Catalog.Application.Products.UpdateProduct;

/// <summary>The SKU is absent on purpose: it is immutable once the product exists.</summary>
public sealed record UpdateProductRequest(
    string? Name,
    string? Description,
    Guid CategoryId,
    decimal BasePrice,
    string? Currency,
    bool IsActive);

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(Product.MaxNameLength);
        RuleFor(request => request.Description).MaximumLength(Product.MaxDescriptionLength);
        RuleFor(request => request.CategoryId).NotEmpty();

        RuleFor(request => request.BasePrice)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, Money.DecimalPlaces, ignoreTrailingZeros: true)
            .WithMessage($"'{{PropertyName}}' must have at most {Money.DecimalPlaces} decimal places.");

        RuleFor(request => request.Currency)
            .NotEmpty()
            .Must(currency => Enum.TryParse<Currency>(currency, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
            .WithMessage($"'{{PropertyName}}' must be one of: {string.Join(", ", Enum.GetNames<Currency>())}.");
    }
}
