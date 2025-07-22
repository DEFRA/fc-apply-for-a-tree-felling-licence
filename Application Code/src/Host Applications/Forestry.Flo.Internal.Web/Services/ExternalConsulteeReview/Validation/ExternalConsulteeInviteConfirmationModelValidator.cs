using FluentValidation;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;

namespace Forestry.Flo.Internal.Web.Services.ExternalConsulteeReview.Validation;

public class ExternalConsulteeInviteConfirmationModelValidator : AbstractValidator<ExternalConsulteeInviteConfirmationModel>
{
    public ExternalConsulteeInviteConfirmationModelValidator()
    {
        RuleFor(m => m.ConfirmedEmail)
            .Must((m, c) => string.Equals(c, m.Email, StringComparison.CurrentCultureIgnoreCase))
            .WithMessage("Confirmed email should match the consultee email.");
    }
}