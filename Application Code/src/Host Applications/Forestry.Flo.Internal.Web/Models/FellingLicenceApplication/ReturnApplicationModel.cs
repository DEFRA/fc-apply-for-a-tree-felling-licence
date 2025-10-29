using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;


public class ReturnApplicationModel : FellingLicenceApplicationPageViewModel
{
    public FellingLicenceStatus RequestedStatus { get; init; }

    /// <summary>
    /// Indicating current user should be able return application
    /// </summary>
    public bool Disabled { get; set; }

    [HiddenInput]
    public Guid ApplicationId { get; set; }

    /// <summary>
    /// Gets and sets a form level case note to provide the reason for returning the application
    /// </summary>
    public FormLevelCaseNote ReturnReason { get; set; }
}