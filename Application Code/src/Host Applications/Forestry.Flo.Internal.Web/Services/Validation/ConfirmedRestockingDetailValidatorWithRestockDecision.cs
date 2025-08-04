using FluentValidation;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using FellingOperationType = Forestry.Flo.Services.FellingLicenceApplications.Entities.FellingOperationType;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class ConfirmedRestockingDetailValidatorWithRestockDecision : AbstractValidator<ConfirmedRestockingDetailViewModel>
{
    public ConfirmedRestockingDetailValidatorWithRestockDecision(IndividualConfirmedRestockingDetailModel? compartment = null)
    {

        RuleFor(m => m.RestockingProposal)
            .NotNull()
            .NotEqual(TypeOfProposal.None)
            .WithMessage(x => "Select the restocking operation type");

        // area to be restocked must be provided, positive and less than confirmed total hectares.
        RuleFor(m => m.RestockArea)
            .NotNull()
            .WithMessage(x => "Enter Area to be restocked")
            .When(m => m.RestockingProposal is not TypeOfProposal.None)
            .DependentRules(() => RuleFor(m => m.RestockArea)
                .GreaterThan(0)
                .WithMessage(x => "Area to be restocked must be a positive value"))
            .When(m => m.RestockingProposal is not TypeOfProposal.None)
            .DependentRules(() => RuleFor(d => d.RestockArea)
                .LessThanOrEqualTo(d => compartment.TotalHectares)
                .WithMessage(x => "Area to be restocked must be less than or equal to the gross size"))
            .When(m => m.RestockingProposal is not TypeOfProposal.None);

        // Require NumberOfTrees to be provided and > 0 for individual trees proposals
        RuleFor(m => m.NumberOfTrees)
            .NotNull()
            .WithMessage("Enter the number of trees to be restocked")
            .When(m => m.RestockingProposal is TypeOfProposal.RestockWithIndividualTrees
                || m.RestockingProposal is TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees);

        RuleFor(m => m.NumberOfTrees)
            .GreaterThan(0)
            .WithMessage("Number of trees must be greater than zero when provided")
            .When(m => m.NumberOfTrees is not null
                && (m.RestockingProposal is TypeOfProposal.RestockWithIndividualTrees
                    || m.RestockingProposal is TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees));

        RuleFor(d => d)
            .Must(d => d.RestockingProposal.HasNoValue() || d.RestockingProposal.Value == TypeOfProposal.None)
            .When(d =>
                d.RestockingProposal.HasValue
                && d.OperationType is not FellingOperationType.None
                && d.OperationType.AllowedRestockingForFellingType(false).Length == 0)
            .WithMessage(x => $"No restocking option should be selected when felling type is {x.OperationType}");

        // ensure restocking density is not null and is greater than 0
        RuleFor(m => m.RestockingDensity)
            .NotNull()
            .WithMessage(x => $"Enter Restocking density")
            .When(m => m.RestockingProposal is not (TypeOfProposal.None or TypeOfProposal.DoNotIntendToRestock))
            .DependentRules(() => RuleFor(m => m.RestockingDensity)
                .GreaterThan(0)
                .WithMessage(x => $"Restocking density must be greater than zero"))
            .When(m => m.RestockingProposal is not (
                TypeOfProposal.None or 
                TypeOfProposal.DoNotIntendToRestock or 
                TypeOfProposal.RestockWithIndividualTrees or 
                TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees or
                TypeOfProposal.CreateDesignedOpenGround));



        // check percent open space is a valid percentage when it has been entered (0 is a valid percentage in this case)
        //RuleFor(m => m.PercentOpenSpace)
        //    .Must(m => m is >= 0 and <= 100)
        //    .WithMessage(x => $"Compartment {compartment.CompartmentName} - Open space must be between zero and 100%")
        //    .When(m => m.PercentOpenSpace is not null);

        // check percent natural regeneration is a valid percentage when it has been entered (0 is a valid percentage in this case)
        //RuleFor(m => m.PercentNaturalRegeneration)
        //    .Must(m => m is >= 0 and <= 100)
        //    .WithMessage(x => $"Compartment {compartment.CompartmentName} - Natural regeneration must be between zero and 100%")
        //    .When(m => m.PercentNaturalRegeneration is not null);

    }
}