using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public class FlaTermsAndConditionsViewModel : ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets or sets the Application id
    /// </summary>
    public override Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the application reference
    /// </summary>
    public string? ApplicationReference { get; set; }

    /// <summary>
    /// Gets or sets the model for displaying breadcrumbs
    /// </summary>
    public BreadcrumbsModel? Breadcrumbs { get; set; }

    /// <summary>
    /// Gets or sets the the task name for displaying in breadcrumbs and step descriptions in audit records
    /// </summary>
    public string TaskName => " Declaration and confirmation";

    /// <summary>
    /// Gets and sets if the terms and conditions have been accepted by the applicant
    /// </summary>
    public bool TermsAndConditionsAccepted { get; set; }

    /// <summary>
    /// Gets and sets if this is a CBW (cricket bat willow) application
    /// </summary>
    public bool IsCBWapplication { get; set; }

    /// <summary>
    /// Gets and sets the total number of trees being restocking in this application
    /// </summary>
    public int TotalNumberOfTreesRestocking { get; set; }
}