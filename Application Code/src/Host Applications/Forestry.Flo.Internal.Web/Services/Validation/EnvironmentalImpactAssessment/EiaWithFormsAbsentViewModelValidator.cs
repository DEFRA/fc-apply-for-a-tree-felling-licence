using FluentValidation;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Validation.EnvironmentalImpactAssessment;

public class EiaWithFormsAbsentViewModelValidator : AbstractValidator<EiaWithFormsAbsentViewModel>
{
    public EiaWithFormsAbsentViewModelValidator()
    {
        RuleFor(d => d.HaveTheFormsBeenReceived)
            .NotNull()
            .WithMessage("Enter whether the EIA forms have been received");

        RuleFor(d => d.EiaProcessInLineWithCode)
            .NotEmpty()
            .When(x => x.HaveTheFormsBeenReceived is true)
            .WithMessage("Enter whether the EIA process is in line with the code");

        RuleFor(d => d.EiaTrackerReferenceNumber)
            .NotEmpty()
            .When(x => x.HaveTheFormsBeenReceived is true)
            .WithMessage("Enter the EIA tracker reference number");
    }
}