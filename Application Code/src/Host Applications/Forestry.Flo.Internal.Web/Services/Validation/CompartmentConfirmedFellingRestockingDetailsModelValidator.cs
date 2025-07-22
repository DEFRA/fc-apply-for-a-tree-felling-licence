using FluentValidation;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class CompartmentConfirmedFellingRestockingDetailsModelValidator : AbstractValidator<CompartmentConfirmedFellingRestockingDetailsModel>
{
    public CompartmentConfirmedFellingRestockingDetailsModelValidator()
    {
        //IMPORTANT - all error messages must start with the compartment name as "Compartment {x.CompartmentName} - etc etc" in order
        //for the error sorting in the controller to function - when adding a new error please make sure you follow this pattern!

        // felling validation
        RuleForEach(m => m.ConfirmedFellingDetails)
            .SetValidator(x => new ConfirmedFellingDetailValidator(x));

        // a positive value must be inputted for confirmed (digitised) total hectares
        RuleFor(m => m.ConfirmedTotalHectares)
            .NotNull()
            .WithMessage(x => $"Compartment {x.CompartmentName} - Confirmed total hectares must be provided")
            .When(m => m.ConfirmedFellingDetails.Any(x => x.OperationType is not FellingOperationType.None))
            .DependentRules(() => RuleFor(m => m.ConfirmedTotalHectares)
                .GreaterThan(0)
                .WithMessage(x => $"Compartment {x.CompartmentName} - Confirmed total hectares must be a positive value"))
            .When(m => m.ConfirmedFellingDetails.Any(x => x.OperationType is not FellingOperationType.None));

    }
}