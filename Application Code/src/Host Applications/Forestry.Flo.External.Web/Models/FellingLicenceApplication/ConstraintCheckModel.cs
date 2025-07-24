using CSharpFunctionalExtensions;
using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public class ConstraintCheckModel: ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
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
    /// Gets or sets the task name for displaying in breadcrumbs and step descriptions in audit records
    /// </summary>
    public string TaskName => " Constraint check";

    /// <summary>
    /// Gets or sets the most recent LIS report for External List Report, this allows the use to download a copy of the document
    /// </summary>
    public Maybe<DocumentModel> MostRecentExternalLisReport { get; set; }

    /// <summary>
    /// Gets or sets if the compartment section Status
    /// </summary>
    public bool? SelectCompartmentStep { get; set; }

    /// <summary>
    /// Gets or sets if the user wants to run the Land information search
    /// </summary>
    public bool? NotRunningExternalLisReport { get; set; }

    /// <summary>
    /// Gets or sets the time of the External List constraint check being run
    /// </summary>
    public DateTime? ExternalLisAccessedTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the flag as to if the External List constraint check is being run
    /// </summary>
    public bool? ExternalLisReportRun { get; set; }

}