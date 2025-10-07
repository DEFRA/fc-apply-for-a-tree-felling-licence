using FluentValidation;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class ConfirmedFellingDetailValidator : AbstractValidator<ConfirmedFellingDetailViewModel>
{
    public ConfirmedFellingDetailValidator(CompartmentConfirmedFellingRestockingDetailsModel? compartment = null)
    {
        //IMPORTANT - all error messages must start with the compartment name as "Compartment {x.CompartmentName} - etc etc" in order
        //for the error sorting in the controller to function - when adding a new error please make sure you follow this pattern!

        // area to be felled must be provided, positive and less than confirmed total hectares.
        RuleFor(m => m.AreaToBeFelled)
            .NotNull()
            .WithMessage(x => $"Compartment {compartment.CompartmentName} - Area to be felled must be provided")
            .When(m => m.OperationType is not FellingOperationType.None)
            .DependentRules(() => RuleFor(m => m.AreaToBeFelled)
                .GreaterThan(0)
                .WithMessage(x => $"Compartment {compartment.CompartmentName} - Area to be felled must be a positive value"))
                .When(m => m.OperationType is not FellingOperationType.None);

        // when felling is selected, at least one confirmed felling species must be provided
        // and the total percentages of these species must equal 100
        RuleFor(m => m.ConfirmedFellingSpecies)
            .Must(m => m.Length > 0)
            .WithMessage(x => $"Compartment {compartment.CompartmentName} - At least one species for felling must be selected")
            .When(m => m.OperationType is not FellingOperationType.None);

        // check number of trees is non-negative, if entered
        RuleFor(m => m.NumberOfTrees)
            .GreaterThan(0)
            .WithMessage(x => $"Compartment {compartment.CompartmentName} - Number of trees must be greater than zero when provided")
            .When(m => m.NumberOfTrees is not null);

        // restocking validation
        RuleForEach(m => m.ConfirmedRestockingDetails)
            .SetValidator(x => new ConfirmedRestockingDetailValidator(compartment, x));

    }
}