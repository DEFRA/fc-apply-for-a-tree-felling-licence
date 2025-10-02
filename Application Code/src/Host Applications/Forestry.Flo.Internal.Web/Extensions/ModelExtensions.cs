using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;
using ProposedRestockingDetailModel = Forestry.Flo.Internal.Web.Models.FellingLicenceApplication.ProposedRestockingDetailModel;

namespace Forestry.Flo.Internal.Web.Extensions;

/// <summary>
/// Extension methods related to model classes within the internal web application.
/// </summary>
public static class ModelExtensions
{
    /// <summary>
    /// Orders a collection of <see cref="FellingAndRestockingDetail"/> by their compartment name - attempting to order by
    /// the numeric portion of the compartment name first, and then alphabetically.
    /// </summary>
    /// <remarks>
    /// This approach assumes primarily numeric compartment names and attempts to correctly sort where this has
    /// a sub-compartment letter e.g. 1a, 1b, 2, 3a, 3b, etc.  If the names are not in this pattern then the
    /// ordering may appear incorrect.
    /// </remarks>
    /// <param name="models">The collection of models to order.</param>
    /// <returns>The collection of models, in order.</returns>
    public static IEnumerable<FellingAndRestockingDetail> OrderByNameNumericOrAlpha(this IEnumerable<FellingAndRestockingDetail> models)
    {
        var allModels = models.ToList();
     
        if (allModels.All(x => double.TryParse(x.CompartmentName, out _)))
        {
            return allModels.OrderBy(x => double.Parse(x.CompartmentName));
        }
        
        return allModels.OrderBy(x => x.CompartmentName.GetFirstNumericComponent() ?? int.MaxValue)
            .ThenBy(x => x.CompartmentName);
    }

    /// <summary>
    /// Orders a collection of <see cref="Models.FellingLicenceApplication.ProposedRestockingDetailModel"/> by their compartment name - attempting to order by
    /// the numeric portion of the compartment name first, and then alphabetically.
    /// </summary>
    /// <remarks>
    /// This approach assumes primarily numeric compartment names and attempts to correctly sort where this has
    /// a sub-compartment letter e.g. 1a, 1b, 2, 3a, 3b, etc.  If the names are not in this pattern then the
    /// ordering may appear incorrect.
    /// </remarks>
    /// <param name="models">The collection of models to order.</param>
    /// <returns>The collection of models, in order.</returns>
    public static IEnumerable<ProposedRestockingDetailModel> OrderByNameNumericOrAlpha(this IEnumerable<ProposedRestockingDetailModel> models)
    {
        var allModels = models.ToList();

        if (allModels.All(x => double.TryParse(x.RestockingCompartmentName ?? string.Empty, out _)))
        {
            return allModels.OrderBy(x => double.Parse(x.RestockingCompartmentName ?? string.Empty));
        }

        return allModels.OrderBy(x => x.RestockingCompartmentName?.GetFirstNumericComponent() ?? int.MaxValue)
            .ThenBy(x => x.RestockingCompartmentName ?? string.Empty);
    }

    /// <summary>
    /// Orders a collection of <see cref="SubmittedCompartmentDesignationsModel"/> by their compartment name - attempting to order by
    /// the numeric portion of the compartment name first, and then alphabetically.
    /// </summary>
    /// <remarks>
    /// This approach assumes primarily numeric compartment names and attempts to correctly sort where this has
    /// a sub-compartment letter e.g. 1a, 1b, 2, 3a, 3b, etc.  If the names are not in this pattern then the
    /// ordering may appear incorrect.
    /// </remarks>
    /// <param name="models">The collection of models to order.</param>
    /// <returns>The collection of models, in order.</returns>
    public static IEnumerable<SubmittedCompartmentDesignationsModel> OrderByNameNumericOrAlpha(this IEnumerable<SubmittedCompartmentDesignationsModel> models)
    {
        var allModels = models.ToList();

        if (allModels.All(x => double.TryParse(x.CompartmentName, out _)))
        {
            return allModels.OrderBy(x => double.Parse(x.CompartmentName));
        }

        return allModels.OrderBy(x => x.CompartmentName.GetFirstNumericComponent() ?? int.MaxValue)
            .ThenBy(x => x.CompartmentName);
    }

    private static int? GetFirstNumericComponent(this string value)
    {
        var output = string.Empty;

        foreach (var nextChar in value)
        {
            if (int.TryParse(nextChar.ToString(), out _) || nextChar == '-' || nextChar == '.')
            {
                output += nextChar;
            }
            else
            {
                // only find the first numeric component of the string
                if (output != string.Empty)
                {
                    break;
                }
            }
        }

        return int.TryParse(output, out var result) ? result : null;
    }
}