using Ardalis.GuardClauses;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.FellingLicenceApplication;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication
{
    public class SupportingDocumentsController : Controller
    {
        private readonly GetSupportingDocumentUseCase _getSupportingDocumentUseCase;
        private readonly RemoveSupportingDocumentUseCase _removeSupportingDocumentUseCase;

        public SupportingDocumentsController(
            GetSupportingDocumentUseCase getSupportingDocumentUseCase,
            RemoveSupportingDocumentUseCase removeSupportingDocumentUseCase)
        {
            ArgumentNullException.ThrowIfNull(removeSupportingDocumentUseCase);
            ArgumentNullException.ThrowIfNull(getSupportingDocumentUseCase);

            _removeSupportingDocumentUseCase = removeSupportingDocumentUseCase;
            _getSupportingDocumentUseCase = getSupportingDocumentUseCase;
        }
        
        public async Task<IActionResult> GetDocument(
            Guid id,
            Guid documentIdentifier,
            CancellationToken cancellationToken)
        {
            return await Execute(id, documentIdentifier, cancellationToken);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadSupportingDocument(
            [FromRoute] Guid id,
            [FromQuery] Guid documentIdentifier,
            CancellationToken cancellationToken)
        {
            return await Execute(id, documentIdentifier, cancellationToken);
        }

        private async Task<IActionResult> Execute(Guid applicationId, Guid documentId, CancellationToken cancellationToken)
        {
            var user = new InternalUser(User);

            var result = await _getSupportingDocumentUseCase.GetSupportingDocumentAsync(user, applicationId, documentId, cancellationToken);

            if (result.IsSuccess)
            {
                return result.Value;
            }

            return RedirectToAction("Error", "Home");
        }


        [HttpPost]
        public async Task<IActionResult> AttachSupportingDocumentation(
            AddSupportingDocumentModel model,
            FormFileCollection supportingDocumentationFiles,
            [FromServices] AddSupportingDocumentsUseCase useCase,
            CancellationToken cancellationToken)
        {
            if (!supportingDocumentationFiles.Any())
                // this shouldn't happen due to javascript checks
                return Redirect(Url.Action("ApplicationSummary", "FellingLicenceApplication", new { id = model.FellingLicenceApplicationId }) +
                                "#supporting-documents-tab");

            var user = new InternalUser(User);

            await useCase.AddDocumentsToApplicationAsync(
                user,
                model.FellingLicenceApplicationId,
                supportingDocumentationFiles,
                ModelState,
                model.AvailableToApplicant,
                model.AvailableToConsultees,
                cancellationToken);

            return Redirect(Url.Action("ApplicationSummary", "FellingLicenceApplication", new { id = model.FellingLicenceApplicationId }) +
                            "#supporting-documents-tab");
        }
        
        public async Task<IActionResult> RemoveSupportingDocumentation(
            Guid id,
            Guid documentIdentifier,
            CancellationToken cancellationToken)
        {
            var user = new InternalUser(User);

            await _removeSupportingDocumentUseCase.RemoveSupportingDocumentsAsync(
                user,
                id,
                documentIdentifier,
                cancellationToken);

            return Redirect(Url.Action("ApplicationSummary", "FellingLicenceApplication", new { id = id }) +
                            "#supporting-documents-tab");
        }
    }
}
