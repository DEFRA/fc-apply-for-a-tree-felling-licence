using FluentValidation;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Services.Validation
{
    public class SelectRestockingOptionsViewModelValidator : AbstractValidator<SelectRestockingOptionsViewModel>
    {
        public SelectRestockingOptionsViewModelValidator()
        {
            RuleFor(m => m.RestockingOptions)
                .Must(m => m.Count > 0)
                .WithMessage("Select at least one option for this compartment");

            RuleFor(m => m.RestockingOptions)
                .Must(m => m.Count == 1)
                .When(m => m.RestockingOptions.Contains(TypeOfProposal.CreateDesignedOpenGround))
                .WithMessage($"When {TypeOfProposal.CreateDesignedOpenGround.GetDisplayName()} is selected, it should be the only restocking option for your selected felling operation");
        }
    }
}
