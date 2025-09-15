using FluentValidation;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Validation.EnvironmentalImpactAssessment;

public class EiaWithFormsPresentViewModelValidator : AbstractValidator<EiaWithFormsPresentViewModel>
{
    public EiaWithFormsPresentViewModelValidator()
    {
        RuleFor(d => d.AreTheFormsCorrect)
            .NotNull()
            .WithMessage("Enter whether the EIA forms are correct");

        RuleFor(d => d.EiaProcessInLineWithCode)
            .NotEmpty()
            .When(x => x.AreTheFormsCorrect is true)
            .WithMessage("Enter whether the EIA process is in line with the code");

        RuleFor(d => d.EiaTrackerReferenceNumber)
            .NotEmpty()
            .When(x => x.AreTheFormsCorrect is true)
            .WithMessage("Enter the EIA tracker reference number");
    }
}