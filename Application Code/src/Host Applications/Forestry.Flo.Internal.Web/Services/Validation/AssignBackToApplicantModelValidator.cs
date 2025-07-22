using FluentValidation;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class AssignBackToApplicantModelValidator : AbstractValidator<AssignBackToApplicantModel>
{
    public AssignBackToApplicantModelValidator()
    {
        RuleFor(v => v.SectionsToReview)
            .Must(x => x.Any(y => y.Value))
            .When(x => !x.LarchCheckSplit)
            .WithMessage("At least one section must be selected for amendment");

        RuleFor(v => v.CompartmentIdentifiersToReview)
            .Must(x => x.Any(y => y.Value))
            .When(x => 
                x.SectionsToReview.ContainsKey(FellingLicenceApplicationSection.FellingAndRestockingDetails) &&
                x.SectionsToReview[FellingLicenceApplicationSection.FellingAndRestockingDetails])
            .WithMessage("At least one compartment must be selected for amendment");
    }
}