using FluentValidation;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class ConfirmedRestockingDetailValidator : AbstractValidator<ConfirmedRestockingDetailViewModel>
{
    public ConfirmedRestockingDetailValidator(CompartmentConfirmedFellingRestockingDetailsModel? compartment = null, ConfirmedFellingDetailViewModel? confirmedFellingDetail = null)
    {
        //IMPORTANT - all error messages must start with the compartment name as "Compartment {x.CompartmentName} - etc etc" in order
        //for the error sorting in the controller to function - when adding a new error please make sure you follow this pattern!

        // felling and restocking operations cannot be set to "none" simultaneously
        RuleFor(d => d.RestockingProposal)
            .Must(d => d is not TypeOfProposal.None)
            .When(d => confirmedFellingDetail.OperationType is FellingOperationType.None)
            .WithMessage(x => $"Compartment {compartment.CompartmentName} - At least one of the felling or restocking operations must be selected")
            .DependentRules(() => RuleFor(m => confirmedFellingDetail.OperationType)
                .Must(m => m is not FellingOperationType.None)
                .WithMessage(x => $"Compartment {compartment.CompartmentName} - At least one of the felling or restocking operations must be selected"))
            .When(m => m.RestockingProposal is TypeOfProposal.None);

        // a valid restocking option must be selected for the selected felling operation type
        RuleFor(d => d)
            .Must(d => confirmedFellingDetail.OperationType.Value.AllowedRestockingForFellingType(false).Contains(d.RestockingProposal.Value))
            .When(d =>
                        d.RestockingProposal.HasValue
                       && confirmedFellingDetail.OperationType.HasValue
                       && confirmedFellingDetail.OperationType is not FellingOperationType.None
                       && confirmedFellingDetail.OperationType.Value.AllowedRestockingForFellingType(false).Length > 0)
            .WithMessage(x => $"Compartment {compartment.CompartmentName} - Felling type {confirmedFellingDetail.OperationType} requires one of the following restocking options to be selected: " + string.Join(',', confirmedFellingDetail.OperationType.Value.AllowedRestockingForFellingType(false).Select(z => z.GetDisplayName())));

        RuleFor(d => d)
            .Must(d => d.RestockingProposal.HasNoValue() || d.RestockingProposal.Value == TypeOfProposal.None)
            .When(d =>
                d.RestockingProposal.HasValue
                && confirmedFellingDetail.OperationType.HasValue
                && confirmedFellingDetail.OperationType is not FellingOperationType.None
                && confirmedFellingDetail.OperationType.Value.AllowedRestockingForFellingType(false).Length == 0)
            .WithMessage(x => $"Compartment {compartment.CompartmentName} - No restocking option should be selected when felling type is {confirmedFellingDetail.OperationType}");

        // ensure restock area is not null, is greater than 0, and is less than confirmed total hectares
        RuleFor(m => m.RestockArea)
            .NotNull()
            .WithMessage(x => $"Compartment {compartment.CompartmentName} - Area to be restocked must be provided")
            .When(m => m.RestockingProposal is not (TypeOfProposal.None or TypeOfProposal.DoNotIntendToRestock))
            .DependentRules(() => RuleFor(m => m.RestockArea)
                .GreaterThan(0)
                .WithMessage(x => $"Compartment {compartment.CompartmentName} - Area to be restocked must be a positive value"))
            .When(m => m.RestockingProposal is not (TypeOfProposal.None or TypeOfProposal.DoNotIntendToRestock));

        // ensure restocking density is not null and is greater than 0
        RuleFor(m => m.RestockingDensity)
            .NotNull()
            .WithMessage(x => $"Compartment {compartment.CompartmentName} - Restocking density must be provided")
            .When(m => m.RestockingProposal is not (TypeOfProposal.None or TypeOfProposal.DoNotIntendToRestock))
            .DependentRules(() => RuleFor(m => m.RestockingDensity)
                .GreaterThan(0)
                .WithMessage(x => $"Compartment {compartment.CompartmentName} - Restocking density must be greater than zero"))
            .When(m => m.RestockingProposal is not (TypeOfProposal.None or TypeOfProposal.DoNotIntendToRestock or TypeOfProposal.RestockWithIndividualTrees or TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees));

        // validate individual restocking species inputs
        RuleForEach(m => m.ConfirmedRestockingSpecies)
            .SetValidator(x => new RestockingSpeciesValidator(compartment.CompartmentName))
            .When(m => m.RestockingProposal is not (TypeOfProposal.None or TypeOfProposal.DoNotIntendToRestock));

        // when restocking is selected, confirmed restocking species must contain at least 1 species
        RuleFor(m => m.ConfirmedRestockingSpecies)
            .Must(m => m.Length > 0)
            .WithMessage(x => $"Compartment {compartment.CompartmentName} - At least one species for restocking must be selected")
            .When(m => m.RestockingProposal is not (TypeOfProposal.None or TypeOfProposal.DoNotIntendToRestock))
            // total of restocking species percentages and percent open space should equal 100 when
            // restocking species percentages don't already sum to 100 (open space inferred to be 0)
            .DependentRules(() => RuleFor(d => d.ConfirmedRestockingSpecies.Select(x => x.Percentage ?? 0).Sum() + (d.PercentOpenSpace ?? 0))
                .Equal(100)
                .WithMessage(x => $"Compartment {compartment.CompartmentName} - Sum of restocking area percentages across species, plus percentage of open space, must total 100%"))
            .When(m => m.RestockingProposal is not (TypeOfProposal.None or TypeOfProposal.DoNotIntendToRestock));

        // check confirmed restocking species is empty when restocking proposal is "none" or "do not intend to restock"
        RuleFor(m => m.ConfirmedRestockingSpecies)
            .Must(m => m.Length == 0)
            .WithMessage(x => $"Compartment {compartment.CompartmentName} - No restocking species should be listed with a restocking proposal of {x.RestockingProposal!.GetDisplayName()}")
            .When(m => m.RestockingProposal is TypeOfProposal.None or TypeOfProposal.DoNotIntendToRestock);

                // check percent open space is a valid percentage when it has been entered (0 is a valid percentage in this case)
                RuleFor(m => m.PercentOpenSpace)
                    .Must(m => m is >= 0 and <= 100)
                    .WithMessage(x => $"Compartment {compartment.CompartmentName} - Open space must be between zero and 100%")
                    .When(m => m.PercentOpenSpace is not null);

                // check percent natural regeneration is a valid percentage when it has been entered (0 is a valid percentage in this case)
                RuleFor(m => m.PercentNaturalRegeneration)
                    .Must(m => m is >= 0 and <= 100)
                    .WithMessage(x => $"Compartment {compartment.CompartmentName} - Natural regeneration must be between zero and 100%")
                    .When(m => m.PercentNaturalRegeneration is not null);

                // When TPO is selected the TPO reference must be added
                RuleFor(m => confirmedFellingDetail.TreePreservationOrderReference)
                    .Must(m => !string.IsNullOrEmpty(m))
                    .WithMessage(x => $"Compartment {compartment.CompartmentName} - Tree Preservation Order Reference must be provided.")
                    .When(m => confirmedFellingDetail.IsPartOfTreePreservationOrder is true);

                // When CA is selected the CA reference must be added
                RuleFor(m => confirmedFellingDetail.ConservationAreaReference)
                    .Must(m => !string.IsNullOrEmpty(m))
                    .WithMessage(x => $"Compartment {compartment.CompartmentName} - Conservation Area Reference must be provided.")
                    .When(m => confirmedFellingDetail.IsWithinConservationArea is true);

    }
}