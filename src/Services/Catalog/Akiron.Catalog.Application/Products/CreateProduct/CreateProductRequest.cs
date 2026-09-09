using Akiron.Catalog.Domain.Pricing;
using Akiron.Catalog.Domain.Products;
using FluentValidation;

namespace Akiron.Catalog.Application.Products.CreateProduct;

public sealed record CreateProductRequest(
    string? Sku,
    string? Name,
    string? Description,
    Guid CategoryId,
    decimal BasePrice,
    string? Currency);

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(request => request.Sku)
            .NotEmpty()
            .Length(Sku.MinLength, Sku.MaxLength)
            .Matches("^[A-Za-z0-9]+(-[A-Za-z0-9]+)*$")
            .WithMessage("'{PropertyName}' must be letters, digits and single hyphens, e.g. 'MICH-205-55-R16'.");

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
