using FluentValidation;
using Forestry.Flo.Services.FellingLicenceApplications.DataImports.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.PropertyProfiles.DataImports;

namespace Forestry.Flo.Services.DataImport.Validators;

public class ProposedFellingSourceValidator : AbstractValidator<ProposedFellingSource>
{
    public ProposedFellingSourceValidator(
        ApplicationSource? linkedApplication,
        PropertyIds? linkedApplicationProperty,
        List<ProposedRestockingSource> restockingForThisFelling,
        List<string> speciesCodes)
    {
        // application id must be in the application source
        RuleFor(s => s.ApplicationId)
            .Must(_ => linkedApplication != null)
            .WithMessage(s => $"Application Id {s.ApplicationId} was not found amongst imported application source records for proposed felling with id {s.ProposedFellingId}");

        // Flov2CompartmentName must be provided...
        RuleFor(s => s.Flov2CompartmentName)
            .NotEmpty()
            .WithMessage(s => $"FLOv2 Compartment Name must be provided for proposed felling with id {s.ProposedFellingId}");

        // ...and must be a valid compartment name for the linked application property
        RuleFor(s => s.Flov2CompartmentName)
            .Must(s => linkedApplicationProperty?.CompartmentIds
                .Any(c => c.CompartmentName.Equals(s, StringComparison.InvariantCultureIgnoreCase)) ?? false)
            .When(s => linkedApplicationProperty != null && !string.IsNullOrEmpty(s.Flov2CompartmentName))
            .WithMessage(s => $"FLOv2 Compartment Name {s.Flov2CompartmentName} not found on linked application property for proposed felling with id {s.ProposedFellingId}");

        // OperationType must not be None
        RuleFor(s => s.OperationType)
            .Must(s => s != FellingOperationType.None)
            .WithMessage(s => $"Felling operation type {s.OperationType} not valid for import process for proposed felling with id {s.ProposedFellingId}");

        // unless this is Thinning or IsRestocking is false, then there should be at least one restocking operation
        RuleFor(s => s.OperationType)
            .Must(_ => restockingForThisFelling.Any())
            .When(s => s.IsRestocking && s.OperationType.AllowedRestockingForFellingType(false).Length > 0)
            .WithMessage(s => $"At least one restocking operation must be provided unless the operation type is Thinning or IsRestocking is false for proposed felling with id {s.ProposedFellingId}");

        // AreaToBeFelled must be less than or equal to the compartment area, if the compartment has an area
        RuleFor(s => s.AreaToBeFelled)
            .Must((f, s) => (linkedApplicationProperty?.CompartmentIds
                .SingleOrDefault(c => c.CompartmentName.Equals(f.Flov2CompartmentName, StringComparison.InvariantCultureIgnoreCase))?.Area ?? s) >= s)
            .When((f, s) => linkedApplicationProperty?.CompartmentIds
                .SingleOrDefault(c => c.CompartmentName.Equals(f.Flov2CompartmentName, StringComparison.InvariantCultureIgnoreCase))?.Area is not null)
            .WithMessage(s => $"Felling area must not be greater than the compartment area for proposed felling with id {s.ProposedFellingId}");

        // AreaToBeFelled must be greater than 0
        RuleFor(s => s.AreaToBeFelled)
            .Must(s => s > 0)
            .WithMessage(s => $"Area to be felled must be greater than zero for proposed felling with id {s.ProposedFellingId}");

        // Number of trees must be provided and >0 when felling individual trees
        RuleFor(s => s.NumberOfTrees)
            .Must(s => s is > 0)
            .When(s => s.OperationType == FellingOperationType.FellingIndividualTrees)
            .WithMessage(s => $"Number of trees must be provided and greater than zero when felling individual trees for proposed felling with id {s.ProposedFellingId}");

        // EstimatedTotalFellingVolume must be greater than 0
        RuleFor(s => s.EstimatedTotalFellingVolume)
            .Must(s => s > 0)
            .WithMessage(s => $"Estimated total felling volume must be greater than zero for proposed felling with id {s.ProposedFellingId}");

        // Tree preservation order reference must be provided when is part of TPO
        RuleFor(s => s.TreePreservationOrderReference)
            .NotEmpty()
            .When(s => s.IsPartOfTreePreservationOrder)
            .WithMessage(s => $"Tree preservation order reference must be provided when the felling is part of a tree preservation order for proposed felling with id {s.ProposedFellingId}");

        // Conservation Area reference must be provided when is within a conservation area
        RuleFor(s => s.ConservationAreaReference)
            .NotEmpty()
            .When(s => s.IsWithinConservationArea)
            .WithMessage(s => $"Conservation area reference must be provided when the felling is part of a conservation area for proposed felling with id {s.ProposedFellingId}");

        // No restocking reason must be provided when IsRestocking is false, unless operation type is Thinning
        RuleFor(s => s.NoRestockingReason)
            .NotEmpty()
            .When(s => s.IsRestocking is false && s.OperationType != FellingOperationType.Thinning)
            .WithMessage(s => $"Reason for not restocking must be provided when not restocking and the operation type is not thinning for proposed felling with id {s.ProposedFellingId}");

        // Species must be provided and valid, unique species codes
        RuleFor(s => s.Species)
            .NotEmpty()
            .DependentRules(() => 
                RuleFor(s => s.Species)
                    .Must(s => s.Split(',').All(species => speciesCodes.Contains(species.Trim())))
                    .WithMessage(s => $"Species {s.Species} contains invalid species codes for proposed felling with id {s.ProposedFellingId}"))
            .DependentRules(() =>
                RuleFor(s => s.Species)
                    .Must(s => s.Split(',').Length == s.Split(',').Distinct().Count())
                    .WithMessage(s => $"Species {s.Species} contains repeated duplicate species codes for proposed felling with id {s.ProposedFellingId}"))
            .WithMessage(s => $"Species must be provided for proposed felling with id {s.ProposedFellingId}");
    }
}