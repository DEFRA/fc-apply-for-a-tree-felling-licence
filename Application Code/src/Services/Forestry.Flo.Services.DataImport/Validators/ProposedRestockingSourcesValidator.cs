using FluentValidation;
using Forestry.Flo.Services.FellingLicenceApplications.DataImports.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.DataImport.Validators;

public class ProposedRestockingSourcesValidator : AbstractValidator<IEnumerable<ProposedRestockingSource>>
{
    public ProposedRestockingSourcesValidator()
    {
        RuleFor(s => s)
            .Must(IsDistinctOperationForFelling)
            .WithMessage("There are repeated restocking operation types for the same felling operation within the Proposed Restocking records source");

        bool IsDistinctOperationForFelling(IEnumerable<ProposedRestockingSource> elements)
        {
            var encounteredCombinations = new HashSet<(int ProposedFellingId, TypeOfProposal restockingType)>();

            foreach (var proposedRestockingSource in elements)
            {
                var combination = (proposedRestockingSource.ProposedFellingId, proposedRestockingSource.RestockingProposal);
                if (!encounteredCombinations.Add(combination))
                {
                    return false; // Duplicate found
                }
            }

            return true;
        }
    }
}