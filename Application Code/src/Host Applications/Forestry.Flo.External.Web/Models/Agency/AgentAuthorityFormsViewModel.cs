using Forestry.Flo.Services.Applicants.Models;

namespace Forestry.Flo.External.Web.Models.Agency;

/// <summary>
/// View model class for the agent authority forms page
/// </summary>
public class AgentAuthorityFormsViewModel : PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets and sets the Agency Id
    /// </summary>
    public Guid AgencyId { get; set; }

    public List<AgentAuthorityModel> AgentAuthorityForms { get; set; }
}