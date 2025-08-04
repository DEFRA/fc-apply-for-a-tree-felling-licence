using FluentValidation;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.PropertyProfiles.Entities;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class AmendConfirmedRestockingDetailsViewModelValidator : AbstractValidator<AmendConfirmedRestockingDetailsViewModel>
{
    public AmendConfirmedRestockingDetailsViewModelValidator()
    {
        RuleFor(m => m.ConfirmedFellingRestockingDetails)
            .SetValidator(new IndividualConfirmedRestockingDetailModelValidator());

        // when restocking is selected, confirmed restocking species must contain at least 1 species
        RuleFor(m => m.Species)
            .Must(m => m.Count > 0)
            .WithMessage(x => $"At least one species for restocking must be selected")
            .When(m => m.ConfirmedFellingRestockingDetails.ConfirmedRestockingDetails.RestockingProposal 
                is not (TypeOfProposal.None or TypeOfProposal.DoNotIntendToRestock or TypeOfProposal.CreateDesignedOpenGround))
            // total of restocking species percentages and percent open space should equal 100 when
            // restocking species percentages don't already sum to 100 (open space inferred to be 0)
            .DependentRules(() => RuleFor(m => m.Species)
                .Must(species => species.Values.Sum(x => x.Percentage ?? 0) == 100)
                .WithMessage(x => $"Sum of restocking area percentages across species, plus percentage of open space, must total 100%"))
            .When(m => m.ConfirmedFellingRestockingDetails.ConfirmedRestockingDetails.RestockingProposal 
                is not (TypeOfProposal.None or TypeOfProposal.DoNotIntendToRestock or TypeOfProposal.CreateDesignedOpenGround));

        // check confirmed restocking species is empty when restocking proposal is "none" or "do not intend to restock"
        RuleFor(m => m.Species)
            .Must(m => m.Count == 0)
            .WithMessage(x => $"No restocking species should be listed with a restocking proposal of {x.ConfirmedFellingRestockingDetails.ConfirmedRestockingDetails.RestockingProposal!.GetDisplayName()}")
            .When(m => m.ConfirmedFellingRestockingDetails.ConfirmedRestockingDetails.RestockingProposal is TypeOfProposal.None or TypeOfProposal.DoNotIntendToRestock or TypeOfProposal.CreateDesignedOpenGround);
    }
}