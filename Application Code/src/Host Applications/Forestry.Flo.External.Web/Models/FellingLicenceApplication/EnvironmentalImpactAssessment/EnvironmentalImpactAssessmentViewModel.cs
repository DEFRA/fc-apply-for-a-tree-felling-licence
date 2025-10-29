namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.EnvironmentalImpactAssessment;

/// <summary>
/// ViewModel representing the Environmental Impact Assessment step in a Felling Licence Application.
/// </summary>
public class EnvironmentalImpactAssessmentViewModel : ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
{
    /// <summary>
    /// Get or sets a list of documents related to the Environmental Impact Assessment (EIA).
    /// </summary>
    public IEnumerable<DocumentModel> EiaDocuments { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the environmental impact assessment application has been completed.
    /// </summary>
    public bool? HasApplicationBeenCompleted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the environmental impact assessment application has been sent.
    /// </summary>
    public bool? HasApplicationBeenSent { get; set; }

    /// <summary>
    /// Gets or sets the reference number of the application.
    /// </summary>
    public string? ApplicationReference { get; set; }

    /// <summary>
    /// Gets or sets the breadcrumbs navigation model for the current page.
    /// </summary>
    public BreadcrumbsModel? Breadcrumbs { get; set; }

    /// <summary>
    /// Gets the name of the current task.
    /// </summary>
    public string TaskName => "Environmental Impact Assessment";

    /// <summary>
    /// Gets or sets the summary information for the Felling Licence Application.
    /// </summary>
    public FellingLicenceApplicationSummary? ApplicationSummary { get; set; }

    /// <summary>
    /// Gets or sets the external link URI for the Environmental Impact Assessment (EIA) application.
    /// </summary>
    public string? EiaApplicationExternalUri { get; set; }
}