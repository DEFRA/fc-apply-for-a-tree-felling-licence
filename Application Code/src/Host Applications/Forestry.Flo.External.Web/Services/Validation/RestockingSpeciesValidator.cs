using FluentValidation;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.External.Web.Services.Validation;

public class RestockingSpeciesValidator : AbstractValidator<KeyValuePair<string, SpeciesModel>>
{
    public RestockingSpeciesValidator()
    {
        RuleFor(v => v.Value)
            .Must(s => s.Percentage != null && s.Percentage != 0)
            .WithMessage((_, s) => $"Enter a percentage for the species {s.SpeciesName} between 0 and 100")
            .WithName(v => $"Species_{v.Value.Species}__Percentage")
            .DependentRules(() => 
                RuleFor(m => m.Value)
                    .Must(m => m.Percentage is > 0.00 and <= 100.00)
                    .WithName(v => $"Species_{v.Value.Species}__Percentage")
                    .WithMessage((_, s) => $"Enter a percentage for the species {s.SpeciesName} between 0 and 100"));
    }
}