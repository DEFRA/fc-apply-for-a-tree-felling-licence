using FluentValidation;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class ConfirmedFellingDetailValidatorWithRestockDecision : AbstractValidator<NewConfirmedFellingDetailViewModel>
{
    public ConfirmedFellingDetailValidatorWithRestockDecision(CompartmentConfirmedFellingRestockingDetailsModelBase? compartment = null)
    {
        FellingOperationType[] restockingNotRequired = [
            FellingOperationType.None,
            FellingOperationType.Thinning
        ];

        RuleFor(m => m.OperationType)
            .NotNull()
            .NotEqual(FellingOperationType.None)
            .WithMessage(x => "Select the felling operation type");

        // area to be felled must be provided, positive and less than confirmed total hectares.
        RuleFor(m => m.AreaToBeFelled)
            .NotNull()
            .WithMessage(x => "Enter the area to be felled")
            .When(m => m.OperationType is not FellingOperationType.None)
            .DependentRules(() => RuleFor(m => m.AreaToBeFelled)
                .GreaterThan(0)
                .WithMessage(x => "Area to be felled must be a positive value"))
            .When(m => m.OperationType is not FellingOperationType.None)
            .DependentRules(() => RuleFor(d => d.AreaToBeFelled)
                .LessThanOrEqualTo(d => compartment.TotalHectares)
                .WithMessage(x => "Area to be felled must be less than or equal to the gross size"))
            .When(m => m.OperationType is not FellingOperationType.None);

        // check number of trees is non-negative, if entered
        RuleFor(m => m.NumberOfTrees)
            .GreaterThan(0)
            .WithMessage(x => "Number of trees must be greater than zero when provided")
            .When(m => m.NumberOfTrees is not null);

        RuleFor(m => m.IsRestocking)
            .NotNull()
            .WithMessage(x => "Select whether restocking is required")
            .When(m =>
                m.OperationType is not null &&
                restockingNotRequired.Contains(m.OperationType.Value) is false);

        RuleFor(m => m.NoRestockingReason)
            .NotEmpty()
            .When(m => 
                m.IsRestocking is false && 
                m.OperationType is not null && 
                restockingNotRequired.Contains(m.OperationType.Value) is false)
            .WithMessage(x => "Enter the reason for no restocking");

        RuleFor(m => m.IsTreeMarkingUsed)
            .NotNull()
            .WithMessage(x => "Select whether tree marking is used")
            .When(m => m.OperationType is not FellingOperationType.None)
            .DependentRules(() => RuleFor(m => m.TreeMarking)
                .NotEmpty()
                .When(m => m.IsTreeMarkingUsed is true)
                .WithMessage(x => "Enter the tree marking reference or description"));

        RuleFor(m => m.IsPartOfTreePreservationOrder)
            .NotNull()
            .WithMessage(x => "Select whether the area is part of a tree preservation order")
            .When(m => m.OperationType is not FellingOperationType.None)
            .DependentRules(() => RuleFor(m => m.TreePreservationOrderReference)
                .NotEmpty()
                .When(m => m.IsPartOfTreePreservationOrder is true)
                .WithMessage(x => "Enter the tree preservation order reference"));

        RuleFor(x => x.IsWithinConservationArea)
            .NotNull()
            .WithMessage(x => "Select whether the area is within a conservation area")
            .When(m => m.OperationType is not FellingOperationType.None)
            .DependentRules(() => RuleFor(m => m.ConservationAreaReference)
                .NotEmpty()
                .When(m => m.IsWithinConservationArea is true)
                .WithMessage(x => "Enter the conservation area reference"));

        RuleFor(m => m.EstimatedTotalFellingVolume)
            .NotNull()
            .WithMessage(x => "Enter the estimated total felling volume")
            .When(m => m.OperationType is not FellingOperationType.None)
            .DependentRules(() => RuleFor(m => m.EstimatedTotalFellingVolume)
                .GreaterThan(0)
                .WithMessage(x => "Estimated total felling volume must be a positive value"))
            .When(m => m.OperationType is not FellingOperationType.None);
    }
}