using FluentValidation;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Services.Validation;

public class ConfirmWithdrawFellingLicenceApplicationViewModelValidator
    : AbstractValidator<ConfirmWithdrawFellingLicenceApplicationViewModel>
{
    public ConfirmWithdrawFellingLicenceApplicationViewModelValidator()
    {
        RuleFor(x => x.WithdrawalReasonOptions)
            .Must(options => options != null && options.Values.Any(selected => selected))
            .WithMessage("Select a reason for withdrawing the application");

        RuleFor(x => x.WithdrawalReasonsOtherDetails)
            .NotEmpty()
            .When(x => x.WithdrawalReasonOptions != null 
                       && x.WithdrawalReasonOptions.TryGetValue(WithdrawalReason.Other, out var otherSelected) 
                       && otherSelected)
            .WithMessage("Enter details of the other reason for withdrawing the application");
    }
}