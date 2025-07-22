using FluentValidation;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class Pw14ChecksViewModelValidator : AbstractValidator<Pw14ChecksViewModel>
{
    public Pw14ChecksViewModelValidator()
    {
        RuleFor(x => x.Pw14Checks)
            .SetValidator(new Pw14ChecksModelValidator())
            .NotNull();
    }
}