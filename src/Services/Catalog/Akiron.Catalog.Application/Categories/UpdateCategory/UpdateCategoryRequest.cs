using Akiron.Catalog.Domain.Categories;
using FluentValidation;

namespace Akiron.Catalog.Application.Categories.UpdateCategory;

/// <summary>The slug is absent on purpose: it is part of public URLs, so changing it silently would break links.</summary>
public sealed record UpdateCategoryRequest(string? Name);

public sealed class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator() =>
        RuleFor(request => request.Name).NotEmpty().MaximumLength(Category.MaxNameLength);
}
