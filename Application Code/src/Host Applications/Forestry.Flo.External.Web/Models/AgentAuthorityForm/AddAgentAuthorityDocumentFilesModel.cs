using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Models.AgentAuthorityForm;

public class AddAgentAuthorityDocumentFilesModel
{
    [HiddenInput]
    public Guid AgentAuthorityId { get; set; }
}
