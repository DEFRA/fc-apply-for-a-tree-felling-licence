using FluentValidation;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.External.Web.Services.Validation;

public class FlaTermsAndConditionsViewModelValidator : AbstractValidator<FlaTermsAndConditionsViewModel>
{
    public FlaTermsAndConditionsViewModelValidator()
    {
        RuleFor(d => d.TermsAndConditionsAccepted)
            .Must(m => m)
            .WithMessage("Accept the declaration and confirmation to continue");
    }
}