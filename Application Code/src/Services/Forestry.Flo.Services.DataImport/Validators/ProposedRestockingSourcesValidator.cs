using FluentValidation;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.DataImports.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;

namespace Forestry.Flo.Services.DataImport.Validators;

public class ProposedRestockingSourcesValidator : AbstractValidator<IEnumerable<ProposedRestockingSource>>
{
    public ProposedRestockingSourcesValidator()
    {
        RuleFor(s => s)
            .Must(IsDistinctSameCompartmentOperationForFelling)
            .WithMessage("There are repeated restocking operation types for the same felling operation within the Proposed Restocking records source");

        RuleFor(s => s)
            .Must(IsDistinctAlternativeCompartmentOperationForFelling)
            .WithMessage("There are repeated restocking operation types for the same restocking compartment and felling operation within the Proposed Restocking records source");

        RuleFor(s => s)
            .Must(DoesNotCombineCreateDesignedOpenGroundWithOtherRestockingOptions)
            .WithMessage($"{TypeOfProposal.CreateDesignedOpenGround.GetDisplayName()} restocking cannot be combined with other restocking options for the same felling operation within the Proposed Restocking records source");

        bool IsDistinctSameCompartmentOperationForFelling(IEnumerable<ProposedRestockingSource> elements)
        {
            var encounteredCombinations = new HashSet<(int ProposedFellingId, TypeOfProposal restockingType)>();

            foreach (var proposedRestockingSource in elements.Where(x => !x.RestockingProposal.IsAlternativeCompartmentRestockingType()))
            {
                var combination = (proposedRestockingSource.ProposedFellingId, proposedRestockingSource.RestockingProposal);
                if (!encounteredCombinations.Add(combination))
                {
                    return false; // Duplicate found
                }
            }

            return true;
        }

        bool IsDistinctAlternativeCompartmentOperationForFelling(IEnumerable<ProposedRestockingSource> elements)
        {
            var encounteredCombinations = new HashSet<(int ProposedFellingId, string? restockingCompartmentName, TypeOfProposal restockingType)>();

            foreach (var proposedRestockingSource in elements.Where(x => x.RestockingProposal.IsAlternativeCompartmentRestockingType()))
            {
                var combination = (proposedRestockingSource.ProposedFellingId, proposedRestockingSource.Flov2CompartmentName, proposedRestockingSource.RestockingProposal);
                if (!encounteredCombinations.Add(combination))
                {
                    return false; // Duplicate found
                }
            }

            return true;
        }

        bool DoesNotCombineCreateDesignedOpenGroundWithOtherRestockingOptions(IEnumerable<ProposedRestockingSource> elements)
        {
            var groupedByFelling = elements.GroupBy(e => e.ProposedFellingId);
            foreach (var group in groupedByFelling)
            {
                if (group.Any(e => e.RestockingProposal == TypeOfProposal.CreateDesignedOpenGround) && group.Count() > 1)
                {
                    return false; // Create Designed Open Ground is combined with other restocking options for the same felling operation
                }
            }
            return true;
        }

    }
}