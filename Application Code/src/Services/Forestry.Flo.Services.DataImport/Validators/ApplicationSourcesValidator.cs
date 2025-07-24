using FluentValidation;
using Forestry.Flo.Services.FellingLicenceApplications.DataImports.Models;

namespace Forestry.Flo.Services.DataImport.Validators;

public class ApplicationSourcesValidator : AbstractValidator<IEnumerable<ApplicationSource>>
{
    public ApplicationSourcesValidator()
    {
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