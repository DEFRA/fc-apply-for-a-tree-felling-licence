using FluentValidation;
using Forestry.Flo.Services.FellingLicenceApplications.DataImports.Models;
using Forestry.Flo.Services.PropertyProfiles.DataImports;

namespace Forestry.Flo.Services.DataImport.Validators;

public class ApplicationSourceValidator : AbstractValidator<ApplicationSource>
{
    public ApplicationSourceValidator(IEnumerable<PropertyIds> propertyIds, DateTime minDate)
    {
        RuleFor(s => s.Flov2PropertyName)
            .Must(s => propertyIds.Any(x => x.Name.Equals(s, StringComparison.InvariantCultureIgnoreCase)))
            .WithMessage(s => $"FLOv2 Property Name {s.Flov2PropertyName} was not found amongst all properties for woodland owner for application with id {s.ApplicationId}");

        RuleFor(s => s.ProposedFellingStart)
            .Must(s => s > DateOnly.FromDateTime(minDate))
            .WithMessage(s => $"Proposed felling start must be in the future for application with id {s.ApplicationId}");

        RuleFor(s => s.ProposedFellingEnd)
            .Must((s, end) => end > s.ProposedFellingStart)
            .WithMessage(s => $"Proposed felling end must be after the proposed felling start for application with id {s.ApplicationId}");
    }
}