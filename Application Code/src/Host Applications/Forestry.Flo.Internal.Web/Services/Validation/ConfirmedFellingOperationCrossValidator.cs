using FluentValidation;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.Common.Extensions;

namespace Forestry.Flo.Internal.Web.Services.Validation;

/// <summary>
/// Cross-entity validator class for confirmed felling operations.
/// </summary>
public class ConfirmedFellingOperationCrossValidator : AbstractValidator<ConfirmedFellingDetailViewModel>
{
    /// <summary>
    /// Creates a new instance of the <see cref="ConfirmedFellingOperationCrossValidator"/> class.
    /// </summary>
    /// <param name="otherFellingOperationsInSameCompartment">A collection of other <see cref="ConfirmedFellingDetailViewModel"/>
    /// felling operation models for the same compartment.</param>
    /// <param name="compartmentName">The name of the compartment that the felling is taking place in.</param>
    public ConfirmedFellingOperationCrossValidator(
        IEnumerable<ConfirmedFellingDetailViewModel> otherFellingOperationsInSameCompartment,
        string compartmentName)
    {
        // Validate that the operation type is unique within the compartment
        RuleFor(x => x.OperationType)
            .Must(x => otherFellingOperationsInSameCompartment.All(o => o.OperationType != x))
            .WithMessage(s => $"There is more than one {s.OperationType.GetDisplayName()} operation in compartment {compartmentName}")
            .WithName(s => $"amend-link-felling-{s.ConfirmedFellingDetailsId}");

        // Validate that if "Is Restocking" is No, then there should be no restocking entries
        RuleFor(x => x.ConfirmedRestockingDetails)
            .Empty()
            .When(x => x.IsRestocking is false)
            .WithMessage(s => $"{s.OperationType.GetDisplayName()} operation in compartment {compartmentName} has 'Is Restocking' set to 'No' but has linked restocking operations")
            .WithName(s => $"amend-link-felling-{s.ConfirmedFellingDetailsId}");

        // Validate if "Is Restocking" is Null (thinning), then there should be no restocking entries
        RuleFor(x => x.ConfirmedRestockingDetails)
            .Empty()
            .When(x => x.IsRestocking is null)
            .WithMessage(s => $"{s.OperationType.GetDisplayName()} operation in compartment {compartmentName} has linked restocking operations")
            .WithName(s => $"amend-link-felling-{s.ConfirmedFellingDetailsId}");

        // Validate that if "Is Restocking" is Yes, then there should be at least one restocking entry
        RuleFor(x => x.ConfirmedRestockingDetails)
            .NotEmpty()
            .When(x => x.IsRestocking is true)
            .WithMessage(s => $"{s.OperationType.GetDisplayName()} operation in compartment {compartmentName} has 'Is Restocking' set to 'Yes' but has no linked restocking operations")
            .WithName(s => $"add-button-restocking-{s.ConfirmedFellingDetailsId}");
    }
}