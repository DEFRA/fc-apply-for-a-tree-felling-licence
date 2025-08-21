using Forestry.Flo.Services.DataImport.Models;

namespace Forestry.Flo.External.Web.Models.DataImport;

public class ParsedDataViewModel : PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// The Woodland Owner Id the user is acting as.
    /// </summary>
    public Guid WoodlandOwnerId { get; set; }

    /// <summary>
    /// The set of parsed data.
    /// </summary>
    public ImportFileSetContents ImportFileSetContents { get; set; }
}