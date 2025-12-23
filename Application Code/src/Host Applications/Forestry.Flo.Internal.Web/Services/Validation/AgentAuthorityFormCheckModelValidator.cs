using FluentValidation;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class AgentAuthorityFormCheckModelValidator : AbstractValidator<AgentAuthorityFormCheckModel>
{
    public AgentAuthorityFormCheckModelValidator()
    {
        RuleFor(x => x.CheckPassed)
            .Must(x => x.HasValue)
            .WithMessage("Select whether the provided AAF is valid");

        RuleFor(x => x.CheckFailedReason)
            .NotEmpty()
            .When(x => x.CheckPassed == false)
            .WithMessage("A reason for the AAF being invalid must be provided");
    }
}