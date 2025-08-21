namespace Forestry.Flo.External.Web.Models.DataImport;

public class DataImportViewModel: PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// The Woodland Owner Id the user is acting as.
    /// </summary>
    public Guid WoodlandOwnerId { get; set; }
}