using FluentValidation;
using Forestry.Flo.Internal.Web.Models.AdminOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class AgentAuthorityFormCheckModelValidator : AbstractValidator<AgentAuthorityFormCheckModel>
{
    public AgentAuthorityFormCheckModelValidator()
    {
        RuleFor(x => x.CheckFailedReason)
            .NotEmpty()
            .When(x => x.CheckPassed == false)
            .WithMessage("Reason for failure must be provided");
    }
}