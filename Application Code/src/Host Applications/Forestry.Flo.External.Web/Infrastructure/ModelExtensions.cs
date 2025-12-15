using Forestry.Flo.External.Web.Models.Compartment;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.External.Web.Infrastructure;

public static class ModelExtensions
{
    /// <summary>
    /// Orders a collection of <see cref="CompartmentModel"/> by their display name - attempting to order by
    /// the numeric portion of the display name first, and then alphabetically.
    /// </summary>
    /// <remarks>
    /// This approach assumes primarily numeric compartment names and attempts to correctly sort where this is
    /// a sub-compartment letter e.g. 1a, 1b, 2, 3a, 3b, etc.  If the names are not in this pattern then the
    /// ordering may appear incorrect.
    /// </remarks>
    /// <param name="models">The collection of models to order.</param>
    /// <returns>The collection of models, in order.</returns>
    public static IEnumerable<CompartmentModel> OrderByNameNumericOrAlpha(this IEnumerable<CompartmentModel> models)
    {
        var allModels = models.ToList();

        if (allModels.All(x => double.TryParse(x.DisplayName ?? string.Empty, out _)))
        {
            return allModels.OrderBy(x => double.Parse(x.DisplayName ?? string.Empty));
        }

        return allModels.OrderBy(x => x.DisplayName?.GetNumericComponent() ?? int.MaxValue)
            .ThenBy(x => x.DisplayName ?? string.Empty);

    }

    /// <summary>
    /// Orders a collection of <see cref="PawsCompartmentDesignationsModel"/> by their compartment name - attempting to order by
    /// the numeric portion of the compartment name first, and then alphabetically.
    /// </summary>
    /// <remarks>
    /// This approach assumes primarily numeric compartment names and attempts to correctly sort where this is
    /// a sub-compartment letter e.g. 1a, 1b, 2, 3a, 3b, etc.  If the names are not in this pattern then the
    /// ordering may appear incorrect.
    /// </remarks>
    /// <param name="models">The collection of models to order.</param>
    /// <returns>The collection of models, in order.</returns>
    public static IEnumerable<PawsCompartmentDesignationsModel> OrderByNameNumericOrAlpha(
        this IEnumerable<PawsCompartmentDesignationsModel> models)
    {
        var allModels = models.ToList();

        if (allModels.All(x => double.TryParse(x.PropertyProfileCompartmentName ?? string.Empty, out _)))
        {
            return allModels.OrderBy(x => double.Parse(x.PropertyProfileCompartmentName ?? string.Empty));
        }

        return allModels.OrderBy(x => x.PropertyProfileCompartmentName?.GetNumericComponent() ?? int.MaxValue)
            .ThenBy(x => x.PropertyProfileCompartmentName ?? string.Empty);
    }

    private static int? GetNumericComponent(this string value)
    {
        var output = string.Empty;

        foreach (var nextChar in value)
        {
            if (int.TryParse(nextChar.ToString(), out _) || nextChar == '-' || nextChar == '.')
            {
                output += nextChar;
            }
        }

        return int.TryParse(output, out var result) ? result : null;
    }
}