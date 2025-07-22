using FluentValidation;
using Forestry.Flo.External.Web.Models.WoodlandOwner;

namespace Forestry.Flo.External.Web.Services.Validation;

public class ManageWoodlandOwnerDetailsValidator : AbstractValidator<ManageWoodlandOwnerDetailsModel>
{
    public ManageWoodlandOwnerDetailsValidator()
    {
        RuleFor(m => m.OrganisationAddress)
            .NotNull()
            .WithMessage("Organisation address must be provided.")
            .When(m => m.IsOrganisation);
        RuleFor(m => m.OrganisationName)
            .NotNull()
            .WithMessage("Organisation name must be provided.")
            .When(m => m.IsOrganisation);
    }
}