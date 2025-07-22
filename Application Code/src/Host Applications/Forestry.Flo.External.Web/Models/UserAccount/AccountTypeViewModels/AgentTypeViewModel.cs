using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Models.UserAccount.AccountTypeViewModels;

/// <summary>
/// View model for selecting the agency type of an agent.
/// </summary>
public class AgentTypeViewModel : PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets and Sets the organisation status of the agent.
    /// </summary>
    [Required]
    public OrganisationStatus? OrganisationStatus { get; set; }
}