using Akiron.Catalog.Domain.Pricing;
using FluentValidation;

namespace Akiron.Catalog.Application.Pricing.CreatePriceGroup;

public sealed record CreatePriceGroupRequest(
    string? Code,
    string? Name,
    string? Currency,
    decimal? DiscountPercentage);

public sealed class CreatePriceGroupRequestValidator : AbstractValidator<CreatePriceGroupRequest>
{
    public CreatePriceGroupRequestValidator()
    {
        RuleFor(request => request.Code)
            .NotEmpty()
            .Length(PriceGroupCode.MinLength, PriceGroupCode.MaxLength)
            .Matches("^[A-Za-z0-9]+(-[A-Za-z0-9]+)*$")
            .WithMessage("'{PropertyName}' must be letters, digits and single hyphens, e.g. 'BAYI-A'.");

        RuleFor(request => request.Name).NotEmpty().MaximumLength(PriceGroup.MaxNameLength);

        RuleFor(request => request.Currency)
            .NotEmpty()
            .Must(currency => Enum.TryParse<Currency>(currency, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
            .WithMessage($"'{{PropertyName}}' must be one of: {string.Join(", ", Enum.GetNames<Currency>())}.");

        RuleFor(request => request.DiscountPercentage!.Value)
            .InclusiveBetween(0m, 100m)
            .PrecisionScale(5, DiscountPercentage.DecimalPlaces, ignoreTrailingZeros: true)
            .When(request => request.DiscountPercentage.HasValue)
            .OverridePropertyName(nameof(CreatePriceGroupRequest.DiscountPercentage))
            .WithMessage($"'{{PropertyName}}' must be between 0 and 100 with at most {DiscountPercentage.DecimalPlaces} decimal places.");
    }
}
