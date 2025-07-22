using Forestry.Flo.Services.Common.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Models.AgentAuthorityForm;

public class AgentAuthorityFormDocumentModel : PageWithBreadcrumbsViewModel
{
    /// <summary>
    /// Gets and sets the Agency Id
    /// </summary>
    public Guid AgencyId { get; set; }

    [HiddenInput]
    public Guid AgentAuthorityId { get; set; }
    public string? WoodlandOwnerOrOrganisationName { get;set; }
    public Guid? WoodlandOwnerId { get;set; }
    public AgentAuthorityFormDocumentItemModel? CurrentAuthorityForm { get; set; }
    public List<AgentAuthorityFormDocumentItemModel> HistoricAuthorityForms {get; set; } = new();
    public bool HasCurrentAuthorityForm => CurrentAuthorityForm != null;
    public bool DoesNotHaveAnyAuthorityForms => CurrentAuthorityForm is null && HistoricAuthorityForms.NotAny();
    public bool HasAuthorityForms => !DoesNotHaveAnyAuthorityForms;
}
