using FluentValidation;
using Forestry.Flo.Services.FellingLicenceApplications.DataImports.Models;

namespace Forestry.Flo.Services.DataImport.Validators;

public class ApplicationSourcesValidator : AbstractValidator<IEnumerable<ApplicationSource>>
{
    public ApplicationSourcesValidator()
    {
        // This rule can be removed if we decide to allow multiple applications in the source file.
        RuleFor(s => s)
            .Must(s => s.ToList().Count == 1)
            .WithMessage("There must be exactly one Application record in the source file");

        RuleFor(s => s)
            .Must(IsDistinctId)
            .WithMessage("There are repeated id values within the Application records source");

        bool IsDistinctId(IEnumerable<ApplicationSource> elements)
        {
            var encounteredIds = new HashSet<int>();

            foreach (var applicationSource in elements)
            {
                if (!encounteredIds.Add(applicationSource.ApplicationId))
                {
                    return false;
                }
            }
            return true;
        }
    }
}