using CSharpFunctionalExtensions;
using FluentValidation;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.TreeHealth;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.External.Web.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Controllers;

public partial class FellingLicenceApplicationController
{
    [HttpGet]
    [EditingAllowed]
    public async Task<IActionResult> TreeHealthCheck(
        Guid applicationId,
        bool? returnToApplicationSummary,
        bool? fromDataImport,
        [FromServices] ICollectTreeHealthIssuesUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var viewModel = await useCase.GetTreeHealthIssuesViewModelAsync(
            applicationId, user, cancellationToken);

        if (viewModel.IsFailure)
        {
            return RedirectToAction(nameof(HomeController.Error), "Home");
        }

        ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = viewModel.Value.ApplicationSummary.WoodlandOwnerId;
        ViewBag.ApplicationSummary = viewModel.Value.ApplicationSummary;
        
        SetTaskBreadcrumbs(viewModel.Value);

        viewModel.Value.ReturnToApplicationSummary = returnToApplicationSummary ?? false;
        viewModel.Value.FromDataImport = fromDataImport ?? false;

        return View(viewModel.Value);
    }

    [HttpPost]
    [EditingAllowed]
    public async Task<IActionResult> TreeHealthCheck(
        Guid applicationId,
        bool? returnToApplicationSummary,
        bool? fromDataImport,
        TreeHealthIssuesViewModel model,
        FormFileCollection treeHealthFiles,
        [FromServices] ICollectTreeHealthIssuesUseCase useCase,
        [FromServices] IValidator<TreeHealthIssuesViewModel> validator,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        ValidateModel(model, validator);

        // Shouldn't upload files if there are no issues
        if (model.TreeHealthIssues.NoTreeHealthIssues && treeHealthFiles.Any())
        {
            ModelState.AddModelError(
                "TreeHealthIssues.TreeHealthIssueSelections",
                "Upload documents only if there are tree health or public safety issues that apply to this application");
        }

        if (!ModelState.IsValid)
        {
            var reloadModel = await ReloadModel();
            return View(reloadModel.Value);
        }

        var saveResult = await useCase.SubmitTreeHealthIssuesAsync(
            applicationId, user, model, treeHealthFiles, cancellationToken);

        if (saveResult.IsFailure)
        {
            switch (saveResult.Error)
            {
                case SubmitTreeHealthIssuesError.DocumentUpload:
                    this.AddErrorMessage("One or more documents could not be uploaded", "tree-health-file-upload");
                    break;
                case SubmitTreeHealthIssuesError.StoreTreeHealthIssues:
                    this.AddErrorMessage("Tree health issues could not be saved",
                        "TreeHealthIssues.TreeHealthIssueSelections");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var reloadModel = await ReloadModel();
            return View(reloadModel.Value);
        }

        if (returnToApplicationSummary == true)
        {
            return RedirectToAction(nameof(ApplicationSummary), new { applicationId });
        }

        if (fromDataImport == true)
        {
            return RedirectToAction(nameof(FellingAndRestockingPlayback), new { applicationId });
        }

        return RedirectToAction(nameof(SelectCompartments), new { applicationId });

        
        async Task<Result<TreeHealthIssuesViewModel>> ReloadModel()
        {
            var reloadModel = await useCase.GetTreeHealthIssuesViewModelAsync(
                applicationId, user, cancellationToken);

            if (reloadModel.IsFailure)
            {
                return Result.Failure<TreeHealthIssuesViewModel>("Could not reload view model");
            }

            reloadModel.Value.TreeHealthIssues.NoTreeHealthIssues = model.TreeHealthIssues.NoTreeHealthIssues;
            reloadModel.Value.TreeHealthIssues.OtherTreeHealthIssue = model.TreeHealthIssues.OtherTreeHealthIssue;
            reloadModel.Value.TreeHealthIssues.OtherTreeHealthIssueDetails = model.TreeHealthIssues.OtherTreeHealthIssueDetails;

            foreach (var issue in reloadModel.Value.TreeHealthIssues.TreeHealthIssueSelections.Keys)
            {
                reloadModel.Value.TreeHealthIssues.TreeHealthIssueSelections[issue]
                    = model.TreeHealthIssues.TreeHealthIssueSelections.TryGetValue(issue, out var selection) && selection;
            }

            ViewData[ViewDataKeyNameConstants.SelectedWoodlandOwnerId] = reloadModel.Value.ApplicationSummary.WoodlandOwnerId;
            ViewBag.ApplicationSummary = reloadModel.Value.ApplicationSummary;

            SetTaskBreadcrumbs(reloadModel.Value);

            reloadModel.Value.ReturnToApplicationSummary = returnToApplicationSummary ?? false;
            reloadModel.Value.FromDataImport = fromDataImport ?? false;

            return reloadModel.Value;
        }
    }

    public async Task<IActionResult> RemoveTreeHealthDocument(
        Guid applicationId,
        Guid documentIdentifier,
        bool? returnToApplicationSummary,
        bool? fromDataImport,
        [FromServices] RemoveSupportingDocumentUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        var removeResult = await useCase.RemoveSupportingDocumentAsync(user, applicationId, documentIdentifier, cancellationToken);

        if (removeResult.IsFailure)
        {
            logger.LogError("Failed to remove tree health document with error {Error}", removeResult.Error);
            this.AddErrorMessage("Could not remove document at this time, try again");
        }
        else
        {
            this.AddConfirmationMessage("Document successfully removed");
        }

        return RedirectToAction(nameof(TreeHealthCheck), new { applicationId, returnToApplicationSummary, fromDataImport });
    }
}