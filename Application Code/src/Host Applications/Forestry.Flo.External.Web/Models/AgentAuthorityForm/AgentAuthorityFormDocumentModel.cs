using Forestry.Flo.Services.Common.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Models.AgentAuthorityForm;

/// <summary>
/// View model for viewing/confirming the agent authority form documents
/// </summary>
public class AgentAuthorityFormDocumentModel : PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets and sets the Agency Id
    /// </summary>
    public Guid AgencyId { get; set; }

    /// <summary>
    /// Gets and sets the Agent Authority Id
    /// </summary>
    [HiddenInput]
    public Guid AgentAuthorityId { get; set; }

    /// <summary>
    /// Gets and sets the name of the woodland owner or organisation
    /// </summary>
    public string? WoodlandOwnerOrOrganisationName { get; set; }

    /// <summary>
    /// Gets and sets the Woodland Owner Id
    /// </summary>
    public Guid? WoodlandOwnerId { get; set; }

    /// <summary>
    /// Gets and sets the current agent authority form, if one exists.
    /// </summary>
    public AgentAuthorityFormDocumentItemModel? CurrentAuthorityForm { get; set; }

    /// <summary>
    /// Gets and sets the historic agent authority forms.
    /// </summary>
    public List<AgentAuthorityFormDocumentItemModel> HistoricAuthorityForms { get; set; } = new();

    /// <summary>
    /// Gets a value indicating whether there is a current authority form.
    /// </summary>
    public bool HasCurrentAuthorityForm => CurrentAuthorityForm != null;

    /// <summary>
    /// Gets a value indicating whether there are no authority forms.
    /// </summary>
    public bool DoesNotHaveAnyAuthorityForms => CurrentAuthorityForm is null && HistoricAuthorityForms.Where(x => x.ValidFromDate > DateTime.Today).NotAny();

    /// <summary>
    /// Gets a value indicating whether there are any authority forms.
    /// </summary>
    public bool HasAuthorityForms => !DoesNotHaveAnyAuthorityForms;
}
