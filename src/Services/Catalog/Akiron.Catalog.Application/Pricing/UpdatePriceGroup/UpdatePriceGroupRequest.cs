using Akiron.Catalog.Domain.Pricing;
using FluentValidation;

namespace Akiron.Catalog.Application.Pricing.UpdatePriceGroup;

/// <summary>
/// Neither the code nor the currency is here. The code addresses the group in URLs and
/// imports; the currency would reinterpret every price already stored in the list.
/// </summary>
public sealed record UpdatePriceGroupRequest(string? Name, decimal? DiscountPercentage);

public sealed class UpdatePriceGroupRequestValidator : AbstractValidator<UpdatePriceGroupRequest>
{
    public UpdatePriceGroupRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(PriceGroup.MaxNameLength);

        RuleFor(request => request.DiscountPercentage!.Value)
            .InclusiveBetween(0m, 100m)
            .PrecisionScale(5, DiscountPercentage.DecimalPlaces, ignoreTrailingZeros: true)
            .When(request => request.DiscountPercentage.HasValue)
            .OverridePropertyName(nameof(UpdatePriceGroupRequest.DiscountPercentage))
            .WithMessage($"'{{PropertyName}}' must be between 0 and 100 with at most {DiscountPercentage.DecimalPlaces} decimal places.");
    }
}
