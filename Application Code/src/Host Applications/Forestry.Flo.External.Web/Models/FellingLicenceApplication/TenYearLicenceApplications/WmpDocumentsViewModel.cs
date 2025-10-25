namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.TenYearLicenceApplications;


/// <summary>
/// View model class for the ten-year licence tasklist step in the felling licence application process.
/// </summary>
public class WmpDocumentsViewModel : ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets or sets a value indicating whether the application was created through data import.
    /// </summary>
    public bool FromDataImport { get; set; }

    /// <summary>
    /// Gets or sets the reference number of the application.
    /// </summary>
    public string? ApplicationReference { get; set; }

    /// <summary>
    /// Get or sets the list of WMP documents for the application
    /// </summary>
    public IEnumerable<DocumentModel> Documents { get; set; } = Enumerable.Empty<DocumentModel>();

    /// <summary>
    /// Gets and sets a <see cref="AddSupportingDocumentModel"/> for adding additional supporting documents to the application.
    /// </summary>
    public AddSupportingDocumentModel AddSupportingDocumentModel { get; set; }

    /// <summary>
    /// Gets or sets the breadcrumbs navigation model for the current page.
    /// </summary>
    public BreadcrumbsModel? Breadcrumbs { get; set; }

    /// <summary>
    /// Gets the name of the current task.
    /// </summary>
    public string TaskName => "10 Year Licence";

    /// <summary>
    /// Gets and sets the application summary details.
    /// </summary>
    public FellingLicenceApplicationSummary ApplicationSummary { get; set; }
}