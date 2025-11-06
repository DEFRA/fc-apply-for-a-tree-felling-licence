using FluentValidation;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class WoodlandOfficerReviewModelValidator : AbstractValidator<WoodlandOfficerReviewModel>
{
    public WoodlandOfficerReviewModelValidator()
    {
        RuleFor(x => x.RecommendedLicenceDuration)
            .NotNull()
            .NotEqual(RecommendedLicenceDuration.None)
            .WithMessage("Select a recommended licence duration");

        RuleFor(x => x.RecommendationForDecisionPublicRegister)
            .NotNull()
            .WithMessage("Select whether you recommend publishing the application to the decision public register");

        RuleFor(x => x.RecommendationForDecisionPublicRegisterReason)
            .NotEmpty()
            .When(x => x.RecommendationForDecisionPublicRegister is false)
            .WithMessage("Provide a reason for the recommendation regarding whether the decision should be published in the public register");
    }
}