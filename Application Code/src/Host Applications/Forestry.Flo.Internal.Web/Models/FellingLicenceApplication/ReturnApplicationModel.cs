using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;


public class ReturnApplicationModel : FellingLicenceApplicationPageViewModel
{
    public FellingLicenceStatus RequestedStatus { get; init; }

    /// <summary>
    /// Indicating current user should be able return application
    /// </summary>
    public bool Disabled { get; set; }

    public string? CaseNote { get; set; }
    public bool VisibleToApplicant { get; set; }
    public bool VisibleToConsultee { get; set; }

    [HiddenInput]
    public Guid ApplicationId { get; set; }
}