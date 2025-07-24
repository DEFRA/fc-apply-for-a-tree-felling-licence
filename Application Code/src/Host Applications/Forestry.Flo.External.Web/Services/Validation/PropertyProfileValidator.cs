using FluentValidation;
using Forestry.Flo.External.Web.Models.PropertyProfile;

namespace Forestry.Flo.External.Web.Services.Validation;

public class PropertyProfileValidator : AbstractValidator<PropertyProfileModel>
{
    public PropertyProfileValidator()
    {
        RuleFor(pp => pp.Name)
            .NotEmpty()
            .WithMessage($"Enter a property name up to {DataValueConstants.PropertyNameMaxLength} characters");
        RuleFor(pp => pp.Name)
            .MaximumLength(DataValueConstants.PropertyNameMaxLength)
            .WithMessage($"Enter a property name up to {DataValueConstants.PropertyNameMaxLength} characters");
        RuleFor(pp => pp.HasWoodlandManagementPlan)
            .NotNull()
            .WithMessage("Select whether this property is covered by a woodland management plan");
        RuleFor(pp => pp.IsWoodlandCertificationScheme)
            .NotNull()
            .WithMessage("Select whether this property is covered by a woodland certification scheme");
        RuleFor(pp => pp.WoodlandManagementPlanReference)
            .NotEmpty()
            .When(pp => pp.HasWoodlandManagementPlan == true)
            .WithMessage("Enter the woodland management plan reference");
        RuleFor(pp => pp.WoodlandCertificationSchemeReference)
            .NotEmpty()
            .When(pp => pp.IsWoodlandCertificationScheme == true)
            .WithMessage("Enter the woodland certification scheme reference");
    }
}