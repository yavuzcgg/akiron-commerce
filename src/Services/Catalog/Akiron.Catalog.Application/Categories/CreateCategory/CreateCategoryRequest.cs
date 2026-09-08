using Akiron.Catalog.Domain.Categories;
using FluentValidation;

namespace Akiron.Catalog.Application.Categories.CreateCategory;

public sealed record CreateCategoryRequest(string? Name, string? Slug);

/// <summary>
/// Turns malformed input into a 400 with per-field messages before the domain sees
/// it. The domain still guards the same rules; this layer only makes the failure
/// friendlier, it is not the thing keeping the invariants true.
/// </summary>
public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(Category.MaxNameLength);

        RuleFor(request => request.Slug)
            .NotEmpty()
            .MaximumLength(Slug.MaxLength)
            .Matches("^[a-z0-9]+(-[a-z0-9]+)*$")
            .WithMessage("'{PropertyName}' must be lowercase letters, digits and single hyphens, e.g. 'kis-lastikleri'.");
    }
}
