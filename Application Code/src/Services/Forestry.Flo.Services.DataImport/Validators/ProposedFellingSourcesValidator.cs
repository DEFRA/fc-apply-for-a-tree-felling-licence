using FluentValidation;
using Forestry.Flo.Services.FellingLicenceApplications.DataImports.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Services.DataImport.Validators;

public class ProposedFellingSourcesValidator : AbstractValidator<IEnumerable<ProposedFellingSource>>
{
    public ProposedFellingSourcesValidator()
    {
        RuleFor(s => s)
            .Must(IsDistinctId)
            .WithMessage("There are repeated id values within the Proposed Felling records source");

        RuleFor(s => s)
            .Must(IsDistinctOperationInCompartmentForApplication)
            .WithMessage("There are repeated felling operation types in the same compartment for the same application within the Proposed Felling records source");

        bool IsDistinctId(IEnumerable<ProposedFellingSource> elements)
        {
            var encounteredIds = new HashSet<int>();

            foreach (var proposedFellingSource in elements)
            {
                if (!encounteredIds.Add(proposedFellingSource.ProposedFellingId))
                {
                    return false; // Duplicate found
                }
            }
            return true;
        }

        bool IsDistinctOperationInCompartmentForApplication(IEnumerable<ProposedFellingSource> elements)
        {
            var encounteredCombinations = new HashSet<(int ApplicationId, string Flov2CompartmentName, FellingOperationType OperationType)>();
            foreach (var proposedFellingSource in elements)
            {
                var combination = (proposedFellingSource.ApplicationId, proposedFellingSource.Flov2CompartmentName, proposedFellingSource.OperationType);
                if (!encounteredCombinations.Add(combination))
                {
                    return false; // Duplicate found
                }
            }
            return true;
        }
    }
}