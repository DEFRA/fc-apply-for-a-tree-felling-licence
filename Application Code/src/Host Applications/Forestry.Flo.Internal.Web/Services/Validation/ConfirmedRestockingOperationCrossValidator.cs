using FluentValidation;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;

namespace Forestry.Flo.Internal.Web.Services.Validation;

/// <summary>
/// Cross-entity validator class for confirmed restocking operations.
/// </summary>
public class ConfirmedRestockingOperationCrossValidator : AbstractValidator<ConfirmedRestockingDetailViewModel>
{
    /// <summary>
    /// Creates a new instance of the <see cref="ConfirmedRestockingOperationCrossValidator"/>.
    /// </summary>
    /// <param name="otherRestockingForSameFellingOperation">A collection of the other <see cref="ConfirmedRestockingDetailViewModel"/>
    /// restocking models for the same felling operation as the one being validated.</param>
    /// <param name="fellingOperation">The <see cref="FellingOperationType"/> of the felling that this restocking is related to.</param>
    /// <param name="fellingCompartmentName">The name of the compartment that the felling is taking place in.</param>
    /// <param name="fellingCompartmentId">The ID of the compartment that the felling is taking place in.</param>
    public ConfirmedRestockingOperationCrossValidator(
        IEnumerable<ConfirmedRestockingDetailViewModel> otherRestockingForSameFellingOperation,
        FellingOperationType fellingOperation,
        string fellingCompartmentName,
        Guid fellingCompartmentId)
    {
        // Validate that the felling compartment ID matches the restocking compartment ID with a same-compartment restocking type
        RuleFor(x => x.RestockingCompartmentId)
            .Equal(fellingCompartmentId)
            .When(s => s.RestockingProposal!.Value.IsAlternativeCompartmentRestockingType() == false)
            .WithMessage(s => $"{s.RestockingProposal.GetDisplayName()} restocking must be in the same compartment as the {fellingOperation.GetDisplayName()} felling operation in compartment {fellingCompartmentName}")
            .WithName(s => $"amend-link-restocking-{s.ConfirmedRestockingDetailsId}");

        // Validate that the felling compartment ID does not match the restocking compartment ID with an alternative-compartment restocking type
        RuleFor(x => x.RestockingCompartmentId)
            .NotEqual(fellingCompartmentId)
            .When(s => s.RestockingProposal!.Value.IsAlternativeCompartmentRestockingType())
            .WithMessage(s => $"{s.RestockingProposal.GetDisplayName()} restocking must be in a different compartment than the {fellingOperation.GetDisplayName()} felling operation in compartment {fellingCompartmentName}")
            .WithName(s => $"amend-link-restocking-{s.ConfirmedRestockingDetailsId}");

        // Validate that the restocking proposal is unique for the felling operation when this is a same-compartment restocking type
        RuleFor(x => x.RestockingProposal)
            .Must(x => otherRestockingForSameFellingOperation.All(o => o.RestockingProposal != x))
            .When(s => s.RestockingProposal!.Value.IsAlternativeCompartmentRestockingType() == false)
            .WithMessage(s => $"{s.RestockingProposal.GetDisplayName()} restocking occurs multiple times for the {fellingOperation.GetDisplayName()} felling operation in compartment {fellingCompartmentName}")
            .WithName(s => $"amend-link-restocking-{s.ConfirmedRestockingDetailsId}");

        // Validate that the restocking proposal is unique for the felling operation and restocking compartment when this is an alternative-compartment restocking type
        RuleFor(x => x.RestockingProposal)
            .Must((x, r) => otherRestockingForSameFellingOperation.All(o => o.RestockingProposal != r || o.RestockingCompartmentId != x.RestockingCompartmentId))
            .When(s => s.RestockingProposal!.Value.IsAlternativeCompartmentRestockingType())
            .WithMessage(s => $"{s.RestockingProposal.GetDisplayName()} restocking occurs multiple times in the same alternative compartment for the {fellingOperation.GetDisplayName()} felling operation in compartment {fellingCompartmentName}")
            .WithName(s => $"amend-link-restocking-{s.ConfirmedRestockingDetailsId}");

        // Validate that the restocking proposal is allowed for the felling operation type
        RuleFor(x => x.RestockingProposal)
            .Must(s => fellingOperation.AllowedRestockingForFellingType(false).Any(r => r == s))
            .WithMessage(s => $"{s.RestockingProposal.GetDisplayName()} restocking is not allowed for the {fellingOperation.GetDisplayName()} felling operation in compartment {fellingCompartmentName}")
            .WithName(s => $"amend-link-restocking-{s.ConfirmedRestockingDetailsId}");
    }
}