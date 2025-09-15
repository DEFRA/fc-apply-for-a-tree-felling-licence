using FluentValidation;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.EnvironmentalImpactAssessment;

namespace Forestry.Flo.External.Web.Services.Validation
{
    public class EnvironmentalImpactAssessmentViewModelValidator : AbstractValidator<EnvironmentalImpactAssessmentViewModel>
    {
        public EnvironmentalImpactAssessmentViewModelValidator()
        {
            // you must specify whether an EIA application has been completed
            RuleFor(d => d.HasApplicationBeenCompleted)
                .NotNull()
                .WithMessage("Enter whether an EIA application has been completed");

            // if the applicant has stated they have completed an EIA application, they must select they have sent it
            RuleFor(x => x.HasApplicationBeenSent)
                .Equal(true)
                .When(y => y.HasApplicationBeenCompleted is true)
                .WithMessage("If you have completed an EIA application, upload it here");
        }
    }
}
