using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication;

public class OperationDetailsModel: ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
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
    public string TaskName => " Operation details";

    /// <summary>
    /// Gets or sets the date at which the application was received.
    /// </summary>
    public DatePart? DateReceived { get; set; } = null!;

    /// <summary>
    /// Gets and sets the source of the application.
    /// </summary>
    public FellingLicenceApplicationSource? ApplicationSource { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating whether the date received should be editable.
    /// </summary>
    public bool DisplayDateReceived { get; set; }

    /// <summary>
    /// Gets or sets the proposed felling start date / time.
    /// </summary>
    public DatePart? ProposedFellingStart { get; set; } = null!;

    /// <summary>
    /// Gets or sets the proposed felling end date / time.
    /// </summary>
    public DatePart? ProposedFellingEnd { get; set; } = null!;

    /// <summary>
    /// Gets or sets the proposed timing
    /// </summary>
    [MaxLength(400)]
    public string? ProposedTiming { get; set; } = null!;

    /// <summary>
    /// Gets or sets the measures
    /// </summary>
    [MaxLength(400)]
    public string? Measures { get; set; } = null!;

    /// <summary>
    /// Gets and sets a flag indicating whether this application is for a ten-year licence, in order
    /// to calculate the back link for this page.
    /// </summary>
    public bool? IsForTenYearLicence { get; set; }
}