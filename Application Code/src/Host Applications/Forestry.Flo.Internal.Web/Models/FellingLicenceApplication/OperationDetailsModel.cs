using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.Common.Models;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class OperationDetailsModel: IApplicationWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets or sets the Application id
    /// </summary>
    public Guid ApplicationId { get; set; }
    
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
    /// Gets or sets the measures
    /// </summary>
    [Required(ErrorMessage = "Measures must be provided")]
    [MaxLength(400)]
    public string? Measures { get; set; } = null!;

    /// <summary>
    /// Gets or sets the proposed felling start date / time.
    /// </summary>
    public DatePart? ProposedFellingStart { get; set; } = null!;

    /// <summary>
    /// Gets or sets the proposed felling end date / time.
    /// </summary>
    public DatePart? ProposedFellingEnd { get; set; } = null!;
}