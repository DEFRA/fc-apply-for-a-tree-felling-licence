using FluentValidation;
using FluentValidation.Results;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Validation;

/// <summary>
/// Validator class for <see cref="ConfirmedFellingRestockingDetailsModel"/> that checks for cross-entity validation errors.
/// </summary>
public class ConfirmedFellingAndRestockingCrossValidator : AbstractValidator<ConfirmedFellingRestockingDetailsModel>
{
    /// <inheritdoc />
    public override async Task<ValidationResult> ValidateAsync(
        ValidationContext<ConfirmedFellingRestockingDetailsModel> context, 
        CancellationToken cancellation = new CancellationToken())
    {
        var baseResult = await base.ValidateAsync(context, cancellation);

        var index = 0;
        foreach (var cpt in context.InstanceToValidate.Compartments)
        {
            index++;
            foreach (var fellingDetail in cpt.ConfirmedFellingDetails)
            {
                var otherFelling = cpt.ConfirmedFellingDetails
                    .Where(x => x.ConfirmedFellingDetailsId != fellingDetail.ConfirmedFellingDetailsId)
                    .ToList();
                var fellingValidator = new ConfirmedFellingOperationCrossValidator(otherFelling, cpt.CompartmentName ?? $"Compartment {index}");

                var fellingErrors = (await fellingValidator.ValidateAsync(fellingDetail, cancellation)).Errors;
                baseResult.Errors.AddRange(fellingErrors);

                foreach (var restockingDetail in fellingDetail.ConfirmedRestockingDetails)
                {
                    var otherRestocking = fellingDetail.ConfirmedRestockingDetails
                        .Where(x => x.ConfirmedRestockingDetailsId != restockingDetail.ConfirmedRestockingDetailsId)
                        .ToList();
                    var restockingValidator = new ConfirmedRestockingOperationCrossValidator(otherRestocking, fellingDetail.OperationType!.Value, cpt.CompartmentName ?? $"Compartment {index}", cpt.SubmittedFlaPropertyCompartmentId!.Value);

                    var restockingErrors = (await restockingValidator.ValidateAsync(restockingDetail, cancellation)).Errors;
                    baseResult.Errors.AddRange(restockingErrors);
                }
            }
        }

        return baseResult;
    }
}