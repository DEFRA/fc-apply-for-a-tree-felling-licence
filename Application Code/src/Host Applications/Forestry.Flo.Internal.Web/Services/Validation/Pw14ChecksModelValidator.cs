using FluentValidation;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class Pw14ChecksModelValidator: AbstractValidator<Pw14ChecksModel>
{
    public Pw14ChecksModelValidator()
    {
        RuleFor(d => d.Pw14ChecksComplete)
            .Must(x => x)
            .WithMessage("All questions on this page must be answered for the woodland officer checks to be completed")
            .WithName("Pw14Checks.Pw14ChecksComplete");
    }
}