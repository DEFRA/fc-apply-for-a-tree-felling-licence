using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Models;

namespace Forestry.Flo.External.Web.Models.FellingLicenceApplication.PawsDesignations;

/// <summary>
/// View model for interacting with the PAWS data in the proposed compartment designations
/// of an application.
/// </summary>
public class PawsDesignationsViewModel : ApplicationStepBase, IApplicationWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets the name of the current task.
    /// </summary>
    public string TaskName => FellingLicenceApplicationSection.PawsAndIawp.GetDescription();

    /// <summary>
    /// Gets or sets the reference number of the application.
    /// </summary>
    public string? ApplicationReference { get; set; }

    /// <summary>
    /// Gets or sets the breadcrumbs navigation model for the current page.
    /// </summary>
    public BreadcrumbsModel? Breadcrumbs { get; set; }

    /// <summary>
    /// Gets and sets the application summary details.
    /// </summary>
    public FellingLicenceApplicationSummary ApplicationSummary { get; set; }

    /// <summary>
    /// Gets and sets the current compartment designation being edited with PAWS data.
    /// </summary>
    public PawsCompartmentDesignationsModel CompartmentDesignation { get; set; }

    /// <summary>
    /// Gets and sets the number of compartments identified as crossing PAWS zones of interest in the application.
    /// </summary>
    public int? PawsCompartmentsCount { get; set; }

    /// <summary>
    /// Gets and sets the number of compartments with completed PAWS data in the application.
    /// </summary>
    public int? PawsCompartmentsCompleteCount { get; set; }
}