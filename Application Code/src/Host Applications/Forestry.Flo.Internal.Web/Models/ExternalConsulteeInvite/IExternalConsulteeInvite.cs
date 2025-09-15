using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

namespace Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;

public interface IExternalConsulteeInvite
{
    /// <summary>
    /// Gets and inits the invitation application id.
    /// </summary>
    public Guid ApplicationId { get; init; }

    /// <summary>
    /// The breadcrumbs data
    /// </summary>
    BreadcrumbsModel? Breadcrumbs { get; set; }

    /// <summary>
    /// The application summary details
    /// </summary>
    FellingLicenceApplicationSummaryModel? FellingLicenceApplicationSummary { get; set; }
}