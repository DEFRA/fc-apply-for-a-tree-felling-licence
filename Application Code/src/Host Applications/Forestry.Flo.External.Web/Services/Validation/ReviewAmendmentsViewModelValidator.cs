using FluentValidation;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.ReviewFellingAndRestockingAmendments;
namespace Forestry.Flo.External.Web.Services.Validation
{
    public class ReviewAmendmentsViewModelValidator : AbstractValidator<ReviewAmendmentsViewModel>
    {
        public ReviewAmendmentsViewModelValidator()
        {
            RuleFor(d => d.ApplicantAgreed)
                .NotNull()
                .WithMessage("Enter whether you agree with the amendments");

            RuleFor(a => a.ApplicantDisagreementReason)
                .NotEmpty()
                .When(a => a.ApplicantAgreed is false)
                .WithMessage("Enter the reason you disagree with the amendments");

            RuleFor(a => a.ApplicantDisagreementReason)
                .MaximumLength(DataValueConstants.ApplicantDisagreementReasonLength)
                .When(a => !string.IsNullOrEmpty(a.ApplicantDisagreementReason))
                .WithMessage($"The reason you disagree with the amendments must be {DataValueConstants.ApplicantDisagreementReasonLength.ToString()} characters or fewer");
        }
    }
}
