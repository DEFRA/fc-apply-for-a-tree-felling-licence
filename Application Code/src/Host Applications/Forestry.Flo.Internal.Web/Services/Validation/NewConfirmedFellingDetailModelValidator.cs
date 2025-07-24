using FluentValidation;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class NewConfirmedFellingDetailModelValidator : AbstractValidator<NewConfirmedFellingDetailModel>
{
    public NewConfirmedFellingDetailModelValidator()
    {
        RuleFor(m => m.ConfirmedFellingDetails)
            .SetValidator(x => new ConfirmedFellingDetailValidatorWithRestockDecision(x));
    }
}