using FluentValidation;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;

namespace Forestry.Flo.External.Web.Services.Validation;

public class ProposedRestockingDetailModelValidator : AbstractValidator<ProposedRestockingDetailModel>
{
    private static readonly TypeOfProposal[] TypesWithNumberOfTrees = 
    [
        TypeOfProposal.RestockWithIndividualTrees, 
        TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees
    ];

    public ProposedRestockingDetailModelValidator()
    {
        RuleFor(m => m.Area)
            .Must(m => m is >0)
            .WithMessage("Enter an area to be restocked in hectares greater than zero")
            .When(m => 
                m.RestockingProposal != TypeOfProposal.None 
                && m.RestockingProposal != TypeOfProposal.DoNotIntendToRestock
                && m.CompartmentTotalHectares.HasNoValue());

        RuleFor(d => d.Area)
            .Must((m, d) => d <= m.CompartmentTotalHectares && d > 0)
            .When(d => d.CompartmentTotalHectares.HasValue)
            .WithMessage("Enter an area to be restocked in hectares greater than zero and less than or equal to the gross size for the compartment");

        RuleFor(m => m.RestockingDensity)
            .Must(m => m is >0)
            .WithMessage("Enter a restocking density greater than zero")
            .When(m => m.RestockingProposal != TypeOfProposal.None
                       && m.RestockingProposal != TypeOfProposal.DoNotIntendToRestock
                       && m.RestockingProposal != TypeOfProposal.RestockWithIndividualTrees
                       && m.RestockingProposal != TypeOfProposal.PlantAnAlternativeAreaWithIndividualTrees
                       && m.RestockingProposal != TypeOfProposal.CreateDesignedOpenGround
                       );

        RuleFor(m => m.NumberOfTrees)
            .Must(m => m is > 0)
            .When(m => TypesWithNumberOfTrees.Contains(m.RestockingProposal))
            .WithMessage($"Enter an estimated number of trees greater than zero");
        
        RuleForEach(m => m.Species)
            .SetValidator(new RestockingSpeciesValidator())
            .When(m => m.RestockingProposal != TypeOfProposal.None 
                       && m.RestockingProposal != TypeOfProposal.DoNotIntendToRestock
                       && m.RestockingProposal != TypeOfProposal.CreateDesignedOpenGround);

        RuleFor(m => m.Species)
            .Must(m => m.Sum(x => x.Value.Percentage) == 100)
            .WithMessage("Enter restocking area percentages that add up to 100")
            .When(m => m.RestockingProposal != TypeOfProposal.None 
                       && m.RestockingProposal != TypeOfProposal.DoNotIntendToRestock
                       && m.RestockingProposal != TypeOfProposal.CreateDesignedOpenGround
                       && m.Species.Any()
                       && m.Species.All(s => s.Value.Percentage is >0.0 and <=100.0));

        RuleFor(m => m.Species)
            .Must(m => m.Any(x => x.Value.IsOpenSpace == false))
            .WithMessage("Select at least one species to restock")
            .When(m => m.RestockingProposal != TypeOfProposal.None 
                       && m.RestockingProposal != TypeOfProposal.DoNotIntendToRestock
                       && m.RestockingProposal != TypeOfProposal.CreateDesignedOpenGround);

        RuleFor(m => m.PercentageEstablishedByCoppiceOrNaturalRegen)
            .NotNull()
            .When(m => m.RestockingProposal.IsCoppiceOrNaturalRegen())
            .WithMessage("Enter the percentage of restocking that will be established with coppice regrowth or natural regeneration");

        RuleFor(m => m.PercentageEstablishedByCoppiceOrNaturalRegen)
            .Must(m => m is >0 and <= 100)
            .When(m => m.RestockingProposal.IsCoppiceOrNaturalRegen() && m.PercentageEstablishedByCoppiceOrNaturalRegen.HasValue)
            .WithMessage("Enter the percentage of restocking that will be established with coppice regrowth or natural regeneration between 0 and 100");

    }
}