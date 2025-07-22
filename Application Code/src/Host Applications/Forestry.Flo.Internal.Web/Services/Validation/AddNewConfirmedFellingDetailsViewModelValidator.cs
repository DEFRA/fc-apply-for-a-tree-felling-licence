using FluentValidation;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class AddNewConfirmedFellingDetailsViewModelValidator : AbstractValidator<AddNewConfirmedFellingDetailsViewModel>
{
    public AddNewConfirmedFellingDetailsViewModelValidator()
    {
        RuleFor(m => m.ConfirmedFellingRestockingDetails)
            .SetValidator(new NewConfirmedFellingDetailModelValidator());

        RuleFor(m => m.Species)
            .Must(m => m.Count > 0)
            .WithMessage("Enter which species of trees will be felled")
            .When(m => m.ConfirmedFellingRestockingDetails.ConfirmedFellingDetails.OperationType is not FellingOperationType.None);
    }
}