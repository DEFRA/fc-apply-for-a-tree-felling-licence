using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models;
using Forestry.Flo.External.Web.Models.AgentAuthorityForm;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.External.Web.Services.AgentAuthority;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Controllers;

[Authorize, RequireCompletedRegistration, UserIsAgentOrAgentAdministrator]
public class AgentAuthorityFormDocumentsController : Controller
{
    /// <summary>
    /// Action which returns the view and data to display a list of all agent authority
    /// forms (current and historical) for the selected agent authority.
    /// </summary>
    /// <param name="agentAuthorityId">The Id of the agent authority to retrieve agent authority forms for.</param>
    /// <param name="useCase">Use-case service to execute</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    [HttpGet]
    [Route("{controller}/{agentAuthorityId}/{action=Index}")]
    public async Task<IActionResult> Index(
        Guid agentAuthorityId,
        [FromServices] GetAgentAuthorityFormDocumentsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await useCase.GetAgentAuthorityFormDocumentsAsync(user, agentAuthorityId, cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Something went wrong getting the authority form information, please try again");
            return RedirectToAction(nameof(Index), "AgentAuthorityForm");
        }

        result.Value.Breadcrumbs = AddFormBreadcrumbs;
        return View(result.Value);
    }

    /// <summary>
    /// Action which returns the files representing the selected agent authority form.
    /// </summary>
    /// <param name="agentAuthorityId">The id of the agent authority to which this form for download belongs.</param>
    /// <param name="agentAuthorityFormId">The id of the agent authority form to get the files of.</param>
    /// <param name="useCase">Use-case service to execute</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    [Route("{controller}/{agentAuthorityId}/{action}/{agentAuthorityFormId}")]
    [HttpGet]
    public async Task<IActionResult> Download(
        Guid agentAuthorityId,
        Guid agentAuthorityFormId,
        [FromServices] DownloadAgentAuthorityFormDocumentUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var result = await useCase.DownloadAgentAuthorityFormDocumentAsync(
            user, 
            agentAuthorityId, 
            agentAuthorityFormId, 
            cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Something went wrong when getting the authority form document for download, please try again");
            return RedirectToAction(nameof(Index), "AgentAuthorityFormDocuments", new { agentAuthorityId });
        }
        return result.Value;
    }

    /// <summary>
    /// Removes the current agent authority form.
    /// <para>The current service implementation does not remove the form from the system,
    /// but updates it with an effective end date resulting with it no longer being the current agent authority form.</para>
    /// </summary>
    /// <param name="agentAuthorityId">The id of the agent authority to which this form for 'removal' belongs.</param>
    /// <param name="agentAuthorityFormId">The id of the agent authority form to 'remove'.</param>
    /// <param name="useCase">Use-case service to execute</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> RemoveCurrentAgentAuthorityForm(
        Guid agentAuthorityId,
        Guid agentAuthorityFormId,
        [FromServices] RemoveAgentAuthorityFormDocumentUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        
        var result = await useCase.RemoveAgentAuthorityDocumentAsync(
            user, 
            agentAuthorityId, 
            agentAuthorityFormId, 
            cancellationToken);

        if (result.IsFailure)
        {
            this.AddErrorMessage("Something went wrong removing the authority form information, please try again");
        }

        return RedirectToAction(nameof(Index), "AgentAuthorityFormDocuments", new { agentAuthorityId});
    }

    /// <summary>
    /// Handles the displaying of the screen to enable an agent to upload
    /// a file or set of files representing a new Agent Authority Document.
    /// </summary>
    /// <param name="id">The id of the agent authority to which the form is to be added to</param>
    /// <returns>Returns view model of <see cref="AddAgentAuthorityDocumentFilesModel"/> for the view to be rendered.</returns>
    [Route("{controller}/{id}/{action}")]
    public IActionResult AddAgentAuthorityFormFiles(Guid id)
    {
        var model = new AddAgentAuthorityDocumentFilesModel
        {
            AgentAuthorityId = id
        };

        return View(model);
    }

    /// <summary>
    /// Handles the form post-back when the multi-part form with the
    /// file attachments is uploaded.
    /// </summary>
    /// <remarks>See the javascript AJAX form submission method present in
    /// the AddAgentAuthorityFormFiles CSHTML.</remarks>
    /// <param name="agentAuthorityId">The id of the agent authority associated with this request to create a new agent authority form.</param>
    /// <param name="agentAuthorityDocumentFiles">The collection of files to be uploaded constituting the AAF.</param>
    /// <param name="useCase">Use-case service to execute</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An <see cref="IActionResult"/> to be handled by the jQuery ajax calls, either returning to the forms listing
    /// page on success via a redirect based on the value contained within the <see cref="CreatedResult"/>, or for the failure outcome jQuery handling on receipt of a <see cref="BadRequestResult"/>.</returns>
    [HttpPost]
    public async Task<IActionResult> AttachAgentAuthorityFiles(
        Guid agentAuthorityId,
        FormFileCollection agentAuthorityDocumentFiles,
        [FromServices] AddAgentAuthorityFormDocumentFilesUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var addFilesResult =  await useCase.AddAgentAuthorityFormDocumentFilesAsync(
            user,
            agentAuthorityId,
            agentAuthorityDocumentFiles,
            cancellationToken);


        if (addFilesResult.IsFailure)
        {
            return new BadRequestResult();
        }

        var returnUrl = Url.Action
        (
            nameof(Index),
            "AgentAuthorityFormDocuments",
            new { agentAuthorityId },
            ControllerContext.HttpContext.Request.Scheme
        );

        return new CreatedResult
        (
            returnUrl!,
            null
        );
    }

    private BreadcrumbsModel AddFormBreadcrumbs => new()
    {
        Breadcrumbs = new List<BreadCrumb>
        {
            new("Home", "Home", "AgentUser", null),
            new("Authority forms", "AgentAuthorityForm", "Index", null),
        },
        CurrentPage = "Add agent authority form"
    };
}
