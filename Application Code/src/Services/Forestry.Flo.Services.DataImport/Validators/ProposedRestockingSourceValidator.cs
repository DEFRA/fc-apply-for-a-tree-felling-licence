using FluentValidation;
using Forestry.Flo.Services.FellingLicenceApplications.DataImports.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.PropertyProfiles.DataImports;

namespace Forestry.Flo.Services.DataImport.Validators;

public class ProposedRestockingSourceValidator : AbstractValidator<ProposedRestockingSource>
{
    public ProposedRestockingSourceValidator(
        ProposedFellingSource? linkedFelling,
        PropertyIds? linkedProperty,
        List<string> speciesCodes)
    {
        // proposedfellingid must be in the proposed felling source
        RuleFor(s => s.ProposedFellingId)
            .Must(_ => linkedFelling != null)
            .WithMessage(s => $"Proposed Felling Id {s.ProposedFellingId} was not found in the Proposed Felling source records for proposed restocking {s.RestockingProposal}");

        // RestockingProposal must valid for the felling type
        RuleFor(s => s.RestockingProposal)
            .Must(s => linkedFelling!.OperationType.AllowedRestockingForFellingType(false).Contains(s))
            .When(s => linkedFelling != null)
            .WithMessage(s => $"Restocking type is not valid for felling type {linkedFelling!.OperationType} for proposed restocking {s.RestockingProposal} with proposed felling id {s.ProposedFellingId}");

        // Flov2CompartmentName must be provided when restocking an alternate area
        // Flov2CompartmentName must be a valid compartment name for the linked property
        // Flov2CompartmentName must not be the same as the felling compartment name for alternative restocking
        RuleFor(s => s.Flov2CompartmentName)
            .NotEmpty()
            .DependentRules(() => 
                RuleFor(s => s.Flov2CompartmentName)
                    .Must(s => linkedProperty?.CompartmentIds
                        .Any(c => c.CompartmentName.Equals(s, StringComparison.InvariantCultureIgnoreCase)) ?? false)
                    .When((s, _) => s.RestockingProposal.IsAlternativeCompartmentRestockingType() && linkedProperty != null)
                    .WithMessage(s => $"Flov2 Compartment Name {s.Flov2CompartmentName} not found on linked property for proposed restocking {s.RestockingProposal} with proposed felling id {s.ProposedFellingId}"))
            .DependentRules(() =>
                RuleFor(s => s.Flov2CompartmentName)
                    .Must(s => linkedFelling!.Flov2CompartmentName.Equals(s, StringComparison.InvariantCultureIgnoreCase) == false)
                    .When(s => s.RestockingProposal.IsAlternativeCompartmentRestockingType() && linkedFelling != null)
                    .WithMessage(s => $"Restocking Compartment {s.Flov2CompartmentName} must not be the same as the felling compartment {linkedFelling!.Flov2CompartmentName} for proposed restocking {s.RestockingProposal} with proposed felling id {s.ProposedFellingId}"))
            .When(s => s.RestockingProposal.IsAlternativeCompartmentRestockingType())
            .WithMessage(s => $"Flov2 Compartment Name must be provided when restocking an alternate area for proposed restocking {s.RestockingProposal} with proposed felling id {s.ProposedFellingId}");


        // AreaToBeRestocked must be less than or equal to the compartment area, if the compartment has an area - same compartment as felling
        RuleFor(s => s.AreaToBeRestocked)
            .Must((f, s) => (linkedProperty?.CompartmentIds
                .SingleOrDefault(c => c.CompartmentName.Equals(linkedFelling!.Flov2CompartmentName, StringComparison.InvariantCultureIgnoreCase))?.Area ?? s) >= s)
            .When((f, s) => 
                linkedFelling != null
                && linkedProperty != null
                && f.RestockingProposal.IsAlternativeCompartmentRestockingType() == false
                && linkedProperty.CompartmentIds.SingleOrDefault(c => c.CompartmentName.Equals(linkedFelling.Flov2CompartmentName, StringComparison.InvariantCultureIgnoreCase))?.Area is not null)
            .WithMessage(s => $"Restocking area must not be greater than the compartment area for proposed restocking {s.RestockingProposal} with proposed felling id {s.ProposedFellingId}");

        // AreaToBeRestocked must be less than or equal to the compartment area, if the compartment has an area - alternative compartment
        RuleFor(s => s.AreaToBeRestocked)
            .Must((f, s) => (linkedProperty?.CompartmentIds
                .SingleOrDefault(c => c.CompartmentName.Equals(f.Flov2CompartmentName, StringComparison.InvariantCultureIgnoreCase))?.Area ?? s) >= s)
            .When((f, s) =>
                linkedProperty != null
                && f.RestockingProposal.IsAlternativeCompartmentRestockingType()
                && linkedProperty.CompartmentIds.SingleOrDefault(c => c.CompartmentName.Equals(f.Flov2CompartmentName, StringComparison.InvariantCultureIgnoreCase))?.Area is not null)
            .WithMessage(s => $"Restocking area must not be greater than the compartment area for proposed restocking {s.RestockingProposal} with proposed felling id {s.ProposedFellingId}");

        // AreaToBeRestocked must be greater than 0
        RuleFor(s => s.AreaToBeRestocked)
            .Must(s => s > 0)
            .WithMessage(s => $"Area to be restocked must be greater than zero for proposed restocking {s.RestockingProposal} with proposed felling id {s.ProposedFellingId}");

        // Restocking density must be provided and > 0 when not restocking individual trees
        RuleFor(s => s.RestockingDensity)
            .Must(s => s is > 0)
            .When(s => s.RestockingProposal.IsNumberOfTreesRestockingType() == false && s.RestockingProposal != TypeOfProposal.CreateDesignedOpenGround)
            .WithMessage(s => $"Restocking density must be provided and greater than zero for proposed restocking {s.RestockingProposal} with proposed felling id {s.ProposedFellingId}");

        // Number of trees must be provided and > 0 when restocking individual trees
        RuleFor(s => s.NumberOfTrees)
            .Must(s => s is > 0)
            .When(s => s.RestockingProposal.IsNumberOfTreesRestockingType())
            .WithMessage(s => $"Number of trees must be provided and greater than zero for proposed restocking {s.RestockingProposal} with proposed felling id {s.ProposedFellingId}");

        RuleFor(s => s.SpeciesAndPercentages)
            .NotEmpty()
            .When(s => s.RestockingProposal != TypeOfProposal.CreateDesignedOpenGround)
            .DependentRules(() =>
                RuleFor(s => s.SpeciesAndPercentages)
                    .Must(s => AllValidSpecies(s, speciesCodes))
                    .When(s => s.RestockingProposal != TypeOfProposal.CreateDesignedOpenGround)
                    .WithMessage(s => $"Species and percentages {s.SpeciesAndPercentages} contains invalid species codes for proposed restocking {s.RestockingProposal} with proposed felling id {s.ProposedFellingId}"))
            .DependentRules(() =>
                RuleFor(s => s.SpeciesAndPercentages)
                    .Must(ValidPercentages)
                    .When(s => s.RestockingProposal != TypeOfProposal.CreateDesignedOpenGround)
                    .WithMessage(s => $"Species and percentages {s.SpeciesAndPercentages} contains invalid percentage or percentages don't total 100% for proposed restocking {s.RestockingProposal} with proposed felling id {s.ProposedFellingId}"))
            .DependentRules(() =>
                RuleFor(s => s.SpeciesAndPercentages)
                    .Must(s => NoRepeatedSpecies(s, speciesCodes))
                    .When(s => s.RestockingProposal != TypeOfProposal.CreateDesignedOpenGround)
                    .WithMessage(s => $"Species and percentages {s.SpeciesAndPercentages} contains repeated duplicate species codes for proposed restocking {s.RestockingProposal} with proposed felling id {s.ProposedFellingId}"))
            .WithMessage(s => $"Species and percentages must be provided for proposed restocking {s.RestockingProposal} with proposed felling id {s.ProposedFellingId}");

    }

    private bool ValidPercentages(string speciesAndPercentages)
    {
        var split = speciesAndPercentages.Split(',', StringSplitOptions.RemoveEmptyEntries);

        var percentages = split.Where((elem, idx) => idx % 2 != 0)
            .Select(elem => elem.Trim())
            .ToList();

        if (percentages.Any(p => !int.TryParse(p, out _)))
        {
            return false;
        }

        var totalPercentage = percentages.Select(int.Parse).Sum();

        return totalPercentage == 100;
    }

    private bool AllValidSpecies(string speciesAndPercentages, List<string> speciesCodes)
    {
        var split = speciesAndPercentages.Split(',', StringSplitOptions.RemoveEmptyEntries);

        var species = split.Where((elem, idx) => idx % 2 == 0);

        return species.All(speciesCode => speciesCodes.Contains(speciesCode.Trim(), StringComparer.InvariantCultureIgnoreCase));
    }

    private bool NoRepeatedSpecies(string speciesAndPercentages, List<string> speciesCodes) 
    {
        var split = speciesAndPercentages.Split(',', StringSplitOptions.RemoveEmptyEntries);

        var species = split.Where((elem, idx) => idx % 2 == 0)
            .Select(x => x.Trim().ToLower()).ToList();

        return species.Count == species.Distinct().Count();
    }
}