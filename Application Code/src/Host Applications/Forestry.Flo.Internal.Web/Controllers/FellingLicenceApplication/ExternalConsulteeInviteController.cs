using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using FluentValidation;
using Forestry.Flo.Internal.Web.Infrastructure;
using Forestry.Flo.Internal.Web.Infrastructure.Display;
using Forestry.Flo.Internal.Web.Models;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;
using Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Internal.Web.Services;
using Forestry.Flo.Internal.Web.Services.ExternalConsulteeReview;
using Forestry.Flo.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Forestry.Flo.Internal.Web.Controllers.FellingLicenceApplication;

 [Authorize]
 [AutoValidateAntiforgeryToken]
public class ExternalConsulteeInviteController : Controller
{
    private readonly IValidator<ExternalConsulteeInviteConfirmationModel> _validator;
    private readonly ExternalConsulteeInviteUseCase _externalConsulteeReviewUseCase;

    public ExternalConsulteeInviteController(
        ExternalConsulteeInviteUseCase externalConsulteeReviewUseCase,
        IValidator<ExternalConsulteeInviteConfirmationModel> validator)
    {
        _validator = Guard.Against.Null(validator);
        _externalConsulteeReviewUseCase =  Guard.Against.Null(externalConsulteeReviewUseCase);
    }

    // GET
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromRoute] Guid? id, 
        [FromQuery] Guid applicationId,
        [FromQuery] string? returnUrl, 
        CancellationToken cancellationToken)
    {

        var (hasValue, inviteModel) = GetInviteModel(id);
        if (!hasValue)
        {
            ClearInviteModels();
        }

        var (_, isFailure, viewModel) = await _externalConsulteeReviewUseCase.RetrieveExternalConsulteeInviteViewModelAsync(
            applicationId,
            inviteModel, 
            returnUrl ?? Url.Action("Index", "Home")!,cancellationToken);

        if (isFailure)
        {
            return RedirectToAction("Error", "Home");
        }

        inviteModel = viewModel.ExternalConsulteeInvite;
        StoreInviteModel(inviteModel);

        viewModel.InviteFormModel.Breadcrumbs = CreateBreadcrumbs(viewModel.InviteFormModel.FellingLicenceApplicationSummary!,
            viewModel.InviteFormModel.ReturnUrl);
        return View(viewModel.InviteFormModel);
    }

    [HttpPost]
    public async Task<IActionResult> Index(
        ExternalConsulteeInviteFormModel model, 
        CancellationToken cancellationToken)
    {
        var (hasValue, inviteModel) = GetInviteModel(model.Id);
        if (!hasValue)
        {
            this.AddErrorMessage(FormDataExpiredError); 
            return RedirectToAction("Index", new { model.ApplicationId, model.ReturnUrl });
        }
        
        if (!ModelState.IsValid)
        {
            StoreInviteModel(inviteModel);
            return await CreateModelView(model, cancellationToken);
        }

        var (_, isFailure, isAlreadyInvited) = await _externalConsulteeReviewUseCase.CheckIfEmailHasAlreadyBeenSentToConsulteeForThisPurposeAsync(
            model, cancellationToken);
        if (isFailure)
        {
            return RedirectToAction("Error", "Home");
        }
        
        inviteModel = inviteModel with
        {
            Email = model.Email, 
            ConsulteeName = model.ConsulteeName, 
            Purpose = model.Purpose, 
            ExternalAccessLink = $"{GetExternalConsulteeInviteLink()}?applicationId={model.ApplicationId}&accessCode={inviteModel.ExternalAccessCode}&emailAddress={model.Email}",
            ExemptFromConsultationPublicRegister = model.ExemptFromConsultationPublicRegister
        };
        StoreInviteModel(inviteModel);
        return RedirectToAction(isAlreadyInvited ? "ReInvite" : "EmailText", new { model.Id, model.ApplicationId, model.ReturnUrl });
    }

    [HttpGet]
    public async Task<IActionResult> EmailText(Guid id, [FromQuery] Guid applicationId, [FromQuery]string returnUrl, CancellationToken cancellationToken)
    {
        var (hasValue, inviteModel) = GetInviteModel(id);
        if (!hasValue)
        {
            this.AddErrorMessage(FormDataExpiredError); 
            return RedirectToAction("Index", new { applicationId, returnUrl });
        }
        var (_, isFailure, viewModel) =
            await _externalConsulteeReviewUseCase.RetrieveExternalConsulteeEmailTextViewModelAsync(applicationId,
                returnUrl, inviteModel, cancellationToken);

        if (isFailure)
        {
            return RedirectToAction("Error", "Home");
        }
        viewModel.Breadcrumbs = CreateBreadcrumbs(viewModel.FellingLicenceApplicationSummary!, returnUrl);
        StoreInviteModel(inviteModel);
        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> EmailText(ExternalConsulteeEmailTextModel model, CancellationToken cancellationToken)
    {
        var (hasValue, inviteModel) = GetInviteModel(model.Id);
        if (!hasValue)
        {
            this.AddErrorMessage(FormDataExpiredError); 
            return RedirectToAction("Index", new { model.ApplicationId, model.ReturnUrl });
        }

        if (!ModelState.IsValid)
        {
            StoreInviteModel(inviteModel);
            return await CreateModelView(model, cancellationToken);
        }
        
        inviteModel = inviteModel with
        {
           ConsulteeEmailText = model.ConsulteeEmailText
        };
        
        StoreInviteModel(inviteModel);
        var routeValues = new { model.Id, model.ApplicationId, model.ReturnUrl};
        return model.ApplicationDocumentsCount > 0 ?
            RedirectToAction("EmailDocuments", routeValues)
            : RedirectToAction("Confirmation", routeValues);
    }

    [HttpGet]
    public async Task<IActionResult> EmailDocuments(Guid id, [FromQuery] Guid applicationId, [FromQuery]string returnUrl, CancellationToken cancellationToken)
    {
        var (hasValue, inviteModel) = GetInviteModel(id);
        if (!hasValue)
        {
            this.AddErrorMessage(FormDataExpiredError); 
            return RedirectToAction("Index", new { applicationId, returnUrl });
        }
        
        var (_, isFailure, viewModel) =
            await _externalConsulteeReviewUseCase.RetrieveExternalConsulteeEmailDocumentsViewModelAsync(applicationId,
                returnUrl, inviteModel, cancellationToken);

        if (isFailure)
        {
            return RedirectToAction("Error", "Home");
        }
        viewModel.Breadcrumbs = CreateBreadcrumbs(viewModel.FellingLicenceApplicationSummary!, returnUrl);
        StoreInviteModel(inviteModel);
        return View(viewModel);
    }

    [HttpPost]
    public IActionResult EmailDocuments(ExternalConsulteeEmailDocumentsModel model)
    {
        var (hasValue, inviteModel) = GetInviteModel(model.Id);
        if (!hasValue)
        {
            this.AddErrorMessage(FormDataExpiredError); 
            return RedirectToAction("Index", new { model.ApplicationId, model.ReturnUrl });
        }

        if (model.SelectedDocumentIds.Count > NotificationConstants.MaxAttachments)
        {
            this.AddErrorMessage($"Please only select up to {NotificationConstants.MaxAttachments} files to attach");
            var routeValues = new { model.Id, model.ApplicationId, model.ReturnUrl };
            return RedirectToAction("EmailDocuments", routeValues);
        }

        inviteModel = inviteModel with
        {
            SelectedDocumentIds = model.SelectedDocumentIds 
        };
        
        StoreInviteModel(inviteModel);
        return RedirectToAction("Confirmation", new { model.Id,  model.ApplicationId, model.ReturnUrl });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(Guid id, [FromQuery] Guid applicationId, [FromQuery]string returnUrl, CancellationToken cancellationToken)
    {
        var (hasValue, inviteModel) = GetInviteModel(id);
        if (!hasValue)
        {
            this.AddErrorMessage(FormDataExpiredError); 
            return RedirectToAction("Index", new { applicationId, returnUrl });
        }
        var user = new InternalUser(User);
        var (_, isFailure, model) =
            await _externalConsulteeReviewUseCase.CreateExternalConsulteeInviteConfirmationAsync(applicationId,
                returnUrl, inviteModel, user, cancellationToken);
        if (isFailure)
        {
            return RedirectToAction("Error", "Home");
        }
        
        model.Breadcrumbs = CreateBreadcrumbs(model.FellingLicenceApplicationSummary!, returnUrl);
        StoreInviteModel(inviteModel);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Confirmation(ExternalConsulteeInviteConfirmationModel model, CancellationToken cancellationToken)
    {
        var (hasValue, inviteModel) = GetInviteModel(model.Id);
        if (!hasValue)
        {
            this.AddErrorMessage(FormDataExpiredError); 
            return RedirectToAction("Index", new { model.ApplicationId, model.ReturnUrl });
        }

        ApplyConfirmedEmailValidationModelErrors(model);
        var user = new InternalUser(User);
        if (!ModelState.IsValid)
        {
            StoreInviteModel(inviteModel);
            return await CreateModelView(model, cancellationToken);
        }

        inviteModel = inviteModel with
        {
            ConsulteeEmailContent = model.EmailContent
        };
        var result =
            await _externalConsulteeReviewUseCase.InviteExternalConsulteeAsync(inviteModel, model.ApplicationId, user, cancellationToken);

        ClearInviteModels();

        if (result.IsSuccess)
        {
            this.AddConfirmationMessage("Consultee invite sent");
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Error", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> ReInvite(Guid id, [FromQuery] Guid applicationId, [FromQuery]string returnUrl, CancellationToken cancellationToken)
    {
        var (hasValue, inviteModel) = GetInviteModel(id);
        if (!hasValue)
        {
            this.AddErrorMessage(FormDataExpiredError); 
            return RedirectToAction("Index", new { applicationId, returnUrl });
        }

        var (_, isFailure, model) =
            await _externalConsulteeReviewUseCase.RetrieveExternalConsulteeReInviteViewModelAsync(applicationId,
                returnUrl, inviteModel, cancellationToken);
        if (isFailure)
        {
            return RedirectToAction("Error", "Home");
        }
        
        model.Breadcrumbs = CreateBreadcrumbs(model.FellingLicenceApplicationSummary!, returnUrl);
        StoreInviteModel(inviteModel);
        return View(model);
    }

    [HttpPost]
    public IActionResult ReInvite(ExternalConsulteeReInviteModel model)
    {
        var (hasValue, inviteModel) = GetInviteModel(model.Id);
        if (!hasValue)
        {
            this.AddErrorMessage(FormDataExpiredError); 
            return RedirectToAction("Index", new { model.ApplicationId, model.ReturnUrl });
        }

        StoreInviteModel(inviteModel);
        return RedirectToAction("EmailText",
            new { model.Id, model.ApplicationId, model.ReturnUrl });
    }
    
    private static BreadcrumbsModel CreateBreadcrumbs(FellingLicenceApplicationSummaryModel summaryModel, string returnUrl, string currentPage = "External Consultee Invite")
    {
        var breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = new List<BreadCrumb> { 
            new("Home", "Home", "Index", null) ,
            new( summaryModel.ApplicationReference,
                "FellingLicenceApplication",
                "ApplicationSummary",
                summaryModel.Id.ToString()),
            new( IsAdminOfficerReview(returnUrl) ?"Operations Admin Officer Review" : "Woodland Officer Review",
                  IsAdminOfficerReview(returnUrl) ?"AdminOfficerReview" : "WoodlandOfficerReview",
                  "Index",
                  summaryModel.Id.ToString())
            },
            CurrentPage = currentPage
        };
        return breadcrumbs;
    }
    
    private const string ConsulteeInviteModel = "ConsulteeInviteModel";

    private void StoreInviteModel(ExternalConsulteeInviteModel viewModel) => 
        TempData[$"{ConsulteeInviteModel}-{viewModel.Id}"] = JsonConvert.SerializeObject(viewModel);

    private Maybe<ExternalConsulteeInviteModel> GetInviteModel(Guid? id)
    {
        if (id is null)
        {
            return Maybe<ExternalConsulteeInviteModel>.None;
        }
        
        return TempData.ContainsKey($"{ConsulteeInviteModel}-{id}")
            ? Maybe<ExternalConsulteeInviteModel>
                .From(JsonConvert.DeserializeObject<ExternalConsulteeInviteModel>(
                    (TempData[$"{ConsulteeInviteModel}-{id}"] as string)!)!)
            : Maybe<ExternalConsulteeInviteModel>.None;
    }

    private void ClearInviteModels()
    {
        var keys = TempData.Keys.Where(k => k.Contains(ConsulteeInviteModel));
        foreach (var key in keys)
        {
            TempData.Remove(key);
        }
    }

    private static bool IsAdminOfficerReview(string viewModelReturnUrl)
    {
        return viewModelReturnUrl.Contains("AdminOfficerReview");
    }

    private void ApplyConfirmedEmailValidationModelErrors(ExternalConsulteeInviteConfirmationModel model)
    {
        if (!ModelState.IsValid) return;
        
        var validationErrors = _validator.Validate(model).Errors;
        if (!validationErrors.Any()) return;

        foreach (var validationFailure in validationErrors)
        {
            ModelState.AddModelError(validationFailure.PropertyName, validationFailure.ErrorMessage);
        }
    }
    
    private async Task<IActionResult> CreateModelView(IExternalConsulteeInvite model, CancellationToken cancellationToken)
    {
        var (_, isFailedInvite, applicationSummary) =
            await _externalConsulteeReviewUseCase.RetrieveApplicationSummaryAsync(model.ApplicationId, cancellationToken);

        if (isFailedInvite)
            return RedirectToAction("Error", "Home");

        model.Breadcrumbs = CreateBreadcrumbs(applicationSummary, model.ReturnUrl);
        model.FellingLicenceApplicationSummary = applicationSummary;
        return View(model);
    }
    
    private string GetExternalConsulteeInviteLink() => Url.AbsoluteAction("Index", "ExternalConsulteeReview")!;
    private const string FormDataExpiredError = "Your form data has expired, please try again";
}