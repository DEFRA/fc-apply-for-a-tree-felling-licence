using FluentValidation;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Validation.EnvironmentalImpactAssessment;

public class EiaScreeningViewModelValidator : AbstractValidator<EiaScreeningViewModel>
{
    public EiaScreeningViewModelValidator()
    {
        RuleFor(d => d.ScreeningCompleted)
            .Must(x => x)
            .WithMessage("Confirm the screening has been completed");
    }
}