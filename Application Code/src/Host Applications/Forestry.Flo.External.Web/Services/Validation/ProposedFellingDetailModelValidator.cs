using FluentValidation;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Services.Validation;

public class ProposedFellingDetailModelValidator : AbstractValidator<ProposedFellingDetailModel>
{
    public ProposedFellingDetailModelValidator()
    {
        RuleFor(m => m.AreaToBeFelled)
            .Must(m => m is >0)
            .WithMessage("Enter an estimated area to be felled in hectares greater than zero")
            .When(m => m.OperationType != FellingOperationType.None && m.CompartmentTotalHectares.HasNoValue());

        RuleFor(d => d.AreaToBeFelled)
            .Must((m, d) => d <= m.CompartmentTotalHectares && d > 0)
            .When(d => d.CompartmentTotalHectares.HasValue)
            .WithMessage("Enter an estimated area to be felled in hectares greater than zero and less than or equal to the gross size for the compartment");
        
        RuleFor(d => d.NumberOfTrees)
            .Must(m => m is > 0)
            .WithMessage("Enter an estimated number of trees to be felled greater than zero")
            .When(m => m.OperationType == FellingOperationType.FellingIndividualTrees);

        RuleFor(d => d.NumberOfTrees)
            .Must(m => m is null or >0)
            .WithMessage("Enter an estimated number of trees to be felled greater than zero")
            .When(m => m.OperationType != FellingOperationType.FellingIndividualTrees);

        RuleFor(d => d.IsTreeMarkingUsed)
            .NotNull()
            .WithMessage("Select whether you will use tree marking")
            .When(m => m.OperationType != FellingOperationType.None);
        RuleFor(d => d.TreeMarking)
            .NotEmpty()
            .When(m => m.IsTreeMarkingUsed == true && m.OperationType != FellingOperationType.None)
            .WithMessage("Enter a tree marking description");
        RuleFor(m => m.Species)
            .Must(m => m.Count > 0)
            .WithMessage("Select at least one species to be felled")
            .When(m => m.OperationType != FellingOperationType.None);
        RuleFor(pp => pp.IsPartOfTreePreservationOrder)
            .NotNull()
            .WithMessage("Select whether there is a tree preservation order (TPO) for any of the trees to be felled")
            .When(pp => pp.OperationType != FellingOperationType.None);
        RuleFor(pp => pp.TreePreservationOrderReference)
            .NotEmpty()
            .When(pp => pp.IsPartOfTreePreservationOrder == true)
            .WithMessage("Enter the tree preservation order reference up to 11 characters")
            .MaximumLength(11)
            .WithMessage("Enter the tree preservation order reference up to 11 characters");
        RuleFor(pp => pp.IsWithinConservationArea)
            .NotNull()
            .WithMessage("Select whether the property is in a conservation area")
            .When(pp => pp.OperationType != FellingOperationType.None);
        RuleFor(pp => pp.ConservationAreaReference)
            .NotEmpty()
            .When(pp => pp.IsWithinConservationArea == true)
            .WithMessage("Enter the conservation area reference up to 20 characters")
            .MaximumLength(20)
            .WithMessage("Enter the conservation area reference up to 20 characters");
        RuleFor(m => m.EstimatedTotalFellingVolume)
            .Must(m => m > 0)
            .WithMessage("Enter an estimated total felling volume greater than zero")
            .When(m => m.OperationType != FellingOperationType.None);
    }
}