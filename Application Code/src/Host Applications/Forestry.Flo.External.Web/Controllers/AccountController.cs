using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Infrastructure.Display;
using Forestry.Flo.External.Web.Models;
using Forestry.Flo.External.Web.Models.Agency;
using Forestry.Flo.External.Web.Models.UserAccount;
using Forestry.Flo.External.Web.Models.UserAccount.AccountTypeViewModels;
using Forestry.Flo.External.Web.Models.WoodlandOwner;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AccountType = Forestry.Flo.External.Web.Models.UserAccount.AccountType;

namespace Forestry.Flo.External.Web.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
[TypeFilter(typeof(ApplicationExceptionFilter))]
public partial class AccountController : Controller
{
    private readonly ValidationProvider _validationProvider;
    private readonly List<BreadCrumb> _breadCrumbsRoot;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        ValidationProvider validationProvider,
        ILogger<AccountController> logger)
    {
        _validationProvider = Guard.Against.Null(validationProvider);
        _logger = logger;

        _breadCrumbsRoot = new List<BreadCrumb>
        {
            new("Home", "Home", "Index", null),
            new("Profile", "Account", "RegisterAccountType", null),
        };
    }

    [HttpGet]
    public async Task<IActionResult> RegisterAccountType([FromQuery] string? token, 
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        UserAccountModel model;
        if (user.HasCompletedAccountRegistration || user.IsAnInvitedUser)
        {  // cannot change account type when completed account

            return RedirectToAction(nameof(AccountSummary));
        }

        if (user.HasRegisteredLocalAccount)
        {
            var existingAccount = await useCase.RetrieveExistingAccountAsync(user, cancellationToken);
            model = GetUserAccountModel(existingAccount, "Account Type");
            return View(model);
        }

        var disableSignUp = true;

        if (string.IsNullOrWhiteSpace(token))
        {
            var userStatusCheckOutcome = await useCase
                .AccountSignupValidityCheck(user, cancellationToken).ConfigureAwait(false);

            if (userStatusCheckOutcome == AccountSignupValidityCheckOutcome.IsValidSignUp)
            {
                disableSignUp = false;
            }
            else
            {
                switch (userStatusCheckOutcome)
                {
                    case AccountSignupValidityCheckOutcome.IsAlreadyInvited:
                        this.AddErrorMessage($"Your organisation has already invited your email address {user.EmailAddress} to sign up with FLO.  To ensure you have access to your organisation's applications, please sign out and then check your emails for an invitation with a link to use to sign up with the system.");
                        break;
                    case AccountSignupValidityCheckOutcome.IsMigratedUser:
                        this.AddErrorMessage($"The email address which you are using to sign in to this service cannot be used at this time. " +
                                             $"To ensure you can access all of your existing information from the previous service, please sign out now " +
                                             $"and await an email invitation from the Forestry Commission which will contain information on signing up to this service.");
                        break;
                    case AccountSignupValidityCheckOutcome.IsDeactivated:
                        //Note: should never get here, as this would have been caught
                        //earlier during checks against the User's claims in Home Controller
                        _logger.LogWarning("A deactivated user account {userId} has arrived at account registration, when should not have been possible, " +
                                           "registration will be disabled.",
                            user.UserAccountId);
                        break;
                }
            }
        }

        model = new UserAccountModel { PageIsDisabled = disableSignUp };
        SetBreadcrumbs(model, "Account Type");
        return View(model);
    }
    
    [HttpPost]
    public async Task<IActionResult> RegisterAccountType(
        UserAccountModel model)
    {
        ApplySectionValidationModelErrors(model, nameof(UserAccountModel.UserTypeModel));

        if (!ModelState.IsValid)
        {
            SetBreadcrumbs(model, "Account Type");
            return View(model);
        }

        return model.UserTypeModel.AccountType switch
        {
            AccountType.Agent => RedirectToAction(nameof(AgentTypeSelection)),
            AccountType.WoodlandOwner => RedirectToAction(nameof(OwnerTypeSelection)),
            AccountType.Tenant => RedirectToAction(nameof(TenantTypeSelection)),
            AccountType.Trust => RedirectToAction(nameof(TrustTypeSelection)),
            _ => RedirectToAction(nameof(RegisterAccountType))
        };
    }

    [HttpGet]
    public async Task<IActionResult> RegisterPersonName(
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var existingAccount = await useCase.RetrieveExistingAccountAsync(user, cancellationToken);
        var model = GetUserAccountModel(existingAccount, "Name");

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> RegisterPersonName(
        UserAccountModel model,
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        ApplySectionValidationModelErrors(model, nameof(AccountPersonNameModel));

        if (!ModelState.IsValid)
        {
            SetBreadcrumbs(model, "Name");
            return View(model);
        }

        var user = new ExternalApplicant(User);
        await useCase.UpdateAccountPersonNameDetailsAsync(user, model, cancellationToken);

        return RedirectToAction(
            user.HasCompletedAccountRegistration ?
            nameof(AccountSummary) :
            nameof(RegisterPersonContactDetails));
    }

    [HttpGet]
    public async Task<IActionResult> RegisterPersonContactDetails(
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var existingAccount = await useCase.RetrieveExistingAccountAsync(user, cancellationToken);
        var model = GetUserAccountModel(existingAccount, "Contact Details");

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> RegisterPersonContactDetails(
        UserAccountModel model,
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        ApplySectionValidationModelErrors(model, nameof(AccountPersonContactModel));

        if (!ModelState.IsValid)
        {
            SetBreadcrumbs(model, "Contact Details");
            return View(model);
        }

        var user = new ExternalApplicant(User);
        await useCase.UpdateAccountPersonContactDetailsAsync(user, model, cancellationToken);

        return RedirectToAction(nameof(AccountSummary));
    }

    [HttpGet]
    public async Task<IActionResult> RegisterOrganisationDetails(
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var existingAccount = await useCase.RetrieveExistingAccountAsync(user, cancellationToken);
        var model = GetWoodlandOwnerModel(existingAccount);

        var viewModel = new WoodlandOwnerOrganisationNameViewModel
        {
            OrganisationName = model.OrganisationName,
            Breadcrumbs = model.Breadcrumbs,
            IsOrganisation = model.IsOrganisation
        };

        return View(viewModel);
    }
    
    [HttpPost]
    public async Task<IActionResult> RegisterOrganisationDetails(
        WoodlandOwnerOrganisationNameViewModel viewModel,
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {

        if (!ModelState.IsValid)
        {
            SetBreadcrumbs(viewModel, "Organisation Details");
            return View(viewModel);
        }

        var user = new ExternalApplicant(User);

        var existingAccount = await useCase.RetrieveExistingAccountAsync(user, cancellationToken);
        var model = GetWoodlandOwnerModel(existingAccount);

        model.OrganisationName = viewModel.OrganisationName;

        await useCase.UpdateAccountWoodlandOwnerDetailsAsync(user, model, cancellationToken);

        return RedirectToAction(
            user.HasCompletedAccountRegistration ?
            nameof(AccountSummary) :
            nameof(RegisterPersonName));
    }

    [HttpGet]
    public async Task<IActionResult> RegisterAgencyDetails(
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var existingAccount = await useCase.RetrieveExistingAccountAsync(user, cancellationToken);
        var model = GetAgencyModel(existingAccount);

        var viewModel = new AgencyOrganisationNameViewModel
        {
            Breadcrumbs = model.Breadcrumbs,
            OrganisationName = model.OrganisationName
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> RegisterAgencyDetails(
        AgencyOrganisationNameViewModel viewModel,
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {

        if (!ModelState.IsValid)
        {
            SetBreadcrumbs(viewModel, "Agency Details");
            return View(viewModel);
        }

        var user = new ExternalApplicant(User);

        var existingAccount = await useCase.RetrieveExistingAccountAsync(user, cancellationToken);
        var model = GetAgencyModel(existingAccount);
        model.OrganisationName = viewModel.OrganisationName;

        await useCase.UpdateUserAgencyDetails(user, model, cancellationToken);

        return RedirectToAction(
            user.HasCompletedAccountRegistration ?
            nameof(AccountSummary) :
            nameof(RegisterPersonName));
    }

    [HttpGet]
    public async Task<IActionResult> AccountSummary(
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var accountTypeReadOnly = user.HasCompletedAccountRegistration || user.IsAnInvitedUser;

        var existingAccount = await useCase.RetrieveExistingAccountAsync(user, cancellationToken);
        if (existingAccount.HasNoValue)
        {
            return RedirectToAction(nameof(RegisterAccountType));
        }

        var model = new UserAccountSummaryModel
        {
            AccountTypeReadOnly = accountTypeReadOnly,
            OrganisationDetailsReadOnly = existingAccount.Value.AccountType != AccountTypeExternal.AgentAdministrator
                && existingAccount.Value.AccountType != AccountTypeExternal.WoodlandOwnerAdministrator
        };

        var accountModel = GetUserAccountModel(existingAccount, "Summary");
        model.Status = accountModel.Status;
        model.PersonName = accountModel.PersonName;
        model.PersonContactsDetails = accountModel.PersonContactsDetails;
        model.UserTypeModel = accountModel.UserTypeModel;

        if (model.UserTypeModel.AccountType is AccountType.WoodlandOwner or AccountType.Trust or AccountType.Tenant)
        {
            model.WoodlandOwner = GetWoodlandOwnerModel(existingAccount);
        }

        if (model.UserTypeModel.AccountType is AccountType.Agent)
        {
            model.Agency = GetAgencyModel(existingAccount);
        }

        if (model.UserTypeModel.AccountType is AccountType.Tenant)
        {
            var firstName = existingAccount.Value.WoodlandOwner?.LandlordFirstName;
            var lastName = existingAccount.Value.WoodlandOwner?.LandlordLastName;
            if (!string.IsNullOrWhiteSpace(firstName) || !string.IsNullOrWhiteSpace(lastName))
            {
                model.LandlordDetails = new LandlordDetails(firstName, lastName);
            }
        }

        return View(model);
    }
    
    [HttpGet]
    public async Task<IActionResult> TermsAndConditions(
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var existingAccount = await useCase.RetrieveExistingAccountAsync(user, cancellationToken);
        var model = GetUserAccountModel(existingAccount, "Terms and conditions");
        SetBreadcrumbs(model, "Terms and conditions");

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> TermsAndConditions(
        UserAccountModel model,
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var termsAcceptance = await useCase.UpdateUserAcceptsTermsAndConditionsAsync(user, model.AcceptsTermsAndConditions,
            cancellationToken);

        if (termsAcceptance.IsSuccess)
        {
            this.AddConfirmationMessage("You have set up your account.");
        }

        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpGet]
    public async Task<IActionResult> ResendInvitationEmail([FromQuery] string invitedUserEmail,
        [FromQuery] string invitedUserName, [FromServices] InviteWoodlandOwnerToOrganisationUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var woodlandOwnerResult = await useCase.RetrieveUserWoodlandOwnerOrganisationAsync(user, cancellationToken);
        if (woodlandOwnerResult.IsFailure)
        {
            this.AddErrorMessage(woodlandOwnerResult.Error);
            return RedirectToAction(nameof(HomeController.WoodlandOwner), "Home");
        }

        var model = new OrganisationWoodlandOwnerUserModel
        {
            WoodlandOwnerId = woodlandOwnerResult.Value.Id,
            WoodlandOwnerName = woodlandOwnerResult.Value.Name,
            Email = invitedUserEmail,
            Name = invitedUserName
        };
        SetBreadcrumbs(model, "Invite Woodland Owner User To Organisation");
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ResendAgentInvitationEmail([FromQuery] string invitedUserEmail,
        [FromQuery] string invitedUserName, [FromServices] InviteAgentToOrganisationUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var agentResult = await useCase.RetrieveUserAgencyAsync(user, cancellationToken);
        if (agentResult.IsFailure)
        {
            this.AddErrorMessage(agentResult.Error);
            return RedirectToAction(nameof(HomeController.WoodlandOwner), "Home");
        }

        var model = new AgencyUserModel
        {
            AgencyId = agentResult.Value.Id,
            AgencyName = agentResult.Value.Name,
            Email = invitedUserEmail,
            Name = invitedUserName
        };
        SetBreadcrumbs(model, "Invite Agent User To Organisation");
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ResendInvitationEmail(OrganisationWoodlandOwnerUserModel model,
        [FromServices] InviteWoodlandOwnerToOrganisationUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var inviteLink = GetEmailInviteLink();

        var result =
            await useCase.ReInviteWoodlandOwnerToOrganisationAsync(model, user, inviteLink, cancellationToken);
        if (result.IsFailure)
        {
            this.AddErrorMessage(result.Error);
            SetBreadcrumbs(model, "Resend invitation link");
            return View(model);
        }

        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [HttpPost]
    public async Task<IActionResult> ResendAgentInvitationEmail(AgencyUserModel model,
        [FromServices] InviteAgentToOrganisationUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var inviteLink = GetAgentEmailInviteLink();

        var result =
            await useCase.ReInviteAgentToOrganisationAsync(model, user, inviteLink, cancellationToken);
        if (result.IsFailure)
        {
            this.AddErrorMessage(result.Error);
            SetBreadcrumbs(model, "Resend invitation link");
            return View(model);
        }

        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    // GET account/InviteUserToOrganisation
    [HttpGet]
    [UserIsInRole(roleName = AccountTypeExternal.WoodlandOwnerAdministrator)]
    public async Task<IActionResult> InviteUserToOrganisation(
        [FromServices] InviteWoodlandOwnerToOrganisationUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var woodlandOwnerResult = await useCase.RetrieveUserWoodlandOwnerOrganisationAsync(user, cancellationToken);
        if (woodlandOwnerResult.IsFailure)
        {
            this.AddErrorMessage(woodlandOwnerResult.Error);
            //TODO: Set the action
            return RedirectToAction(nameof(HomeController.WoodlandOwner), "Home");
        }

        var model = new OrganisationWoodlandOwnerUserModel
        {
            WoodlandOwnerId = woodlandOwnerResult.Value.Id,
            WoodlandOwnerName = woodlandOwnerResult.Value.Name
        };
        SetBreadcrumbs(model, "Invite another user");
        return View(model);
    }
    
    // GET account/InviteAgentToOrganisation
    [HttpGet]
    [UserIsInRole(roleName = AccountTypeExternal.AgentAdministrator)]
    public async Task<IActionResult> InviteAgentToOrganisation(
        [FromServices] InviteAgentToOrganisationUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        if (user.IsFcUser)
        {
            this.AddErrorMessage("Forestry Commission users should sign up to the Internal site which will automatically create their external applicant site account.");
            return RedirectToAction(nameof(HomeController.AgentUser), "Home");
        }

        var agencyResult = await useCase.RetrieveUserAgencyAsync(user, cancellationToken);
        if (agencyResult.IsFailure)
        {
            this.AddErrorMessage(agencyResult.Error);
            return RedirectToAction(nameof(HomeController.AgentUser), "Home");
        }

        var model = new AgencyUserModel
        {
            AgencyId = agencyResult.Value.Id,
            AgencyName = agencyResult.Value.Name
        };
        SetBreadcrumbs(model, "Invite Agent To Organisation");
        return View(model);
    }

    // POST account/InviteUserToOrganisation
    [HttpPost]
    [UserIsInRole(roleName = AccountTypeExternal.AgentAdministrator)]
    public async Task<IActionResult> InviteAgentToOrganisation(
        AgencyUserModel model,
        [FromServices] InviteAgentToOrganisationUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);

        if (user.IsFcUser)
        {
            this.AddErrorMessage("Forestry Commission users should sign up to the Internal site which will automatically create their external applicant site account.");
            return RedirectToAction(nameof(HomeController.AgentUser), "Home");
        }

        if (!ModelState.IsValid)
        {
            SetBreadcrumbs(model, "Invite Agent To Organisation");
            return View(model);
        }

        var inviteLink = GetAgentEmailInviteLink();

        var result =
            await useCase.InviteAgentToOrganisationAsync(model, user, inviteLink, cancellationToken);
        switch (result.IsFailure)
        {
            case true when result.Error.ErrorResult is InviteUserErrorResult.OperationFailed or InviteUserErrorResult.UserAlreadyExists:
                this.AddErrorMessage(result.Error.Message);
                SetBreadcrumbs(model, "Invite Agent To Organisation");
                return View(model);
            case true when result.Error.ErrorResult == InviteUserErrorResult.UserAlreadyInvited:
                return RedirectToAction(nameof(ResendAgentInvitationEmail),
                    new { invitedUserEmail = model.Email, invitedUserName = model.Name });
            default:
                this.AddConfirmationMessage("Invitation sent successfully.");
                return RedirectToAction(nameof(HomeController.Index), "Home");
        }
    }

    private string GetEmailInviteLink() => Url.AbsoluteAction("AcceptInvitation", "Home")!;
    private string GetAgentEmailInviteLink() => Url.AbsoluteAction("AcceptAgentInvitation", "Home")!;

    // POST account/InviteUserToOrganisation
    [HttpPost]
    [UserIsInRole(roleName = AccountTypeExternal.WoodlandOwnerAdministrator)]
    public async Task<IActionResult> InviteUserToOrganisation(OrganisationWoodlandOwnerUserModel model,
        [FromServices] InviteWoodlandOwnerToOrganisationUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            SetBreadcrumbs(model, "Invite another user");
            return View(model);
        }

        var user = new ExternalApplicant(User);
        var inviteLink = GetEmailInviteLink();

        var result =
            await useCase.InviteWoodlandOwnerToOrganisationAsync(model, user, inviteLink, cancellationToken);
        switch (result.IsFailure)
        {
            case true when result.Error.ErrorResult is InviteUserErrorResult.OperationFailed or InviteUserErrorResult.UserAlreadyExists:
                this.AddErrorMessage(result.Error.Message);
                SetBreadcrumbs(model, "Invite Woodland Owner User To Organisation");
                return View(model);
            case true when result.Error.ErrorResult == InviteUserErrorResult.UserAlreadyInvited:
                return RedirectToAction(nameof(ResendInvitationEmail),
                    new { invitedUserEmail = model.Email, invitedUserName = model.Name });
            default:
                this.AddConfirmationMessage("Invitation sent successfully.");
                return RedirectToAction(nameof(LinkedUsersController.WoodlandOwnerUsers), "LinkedUsers");
        }
    }

    private void ApplySectionValidationModelErrors(UserAccountModel userAccountModel, string modelPart)
    {
        var sectionValidationErrors = _validationProvider.ValidateSection(userAccountModel, modelPart, ModelState);

        if (!sectionValidationErrors.Any()) return;

        foreach (var validationFailure in sectionValidationErrors)
        {
            ModelState.AddModelError(validationFailure.PropertyName, validationFailure.ErrorMessage);
        }
    }

    private UserAccountModel GetUserAccountModel(Maybe<UserAccount> existingAccount, string pageName)
    {
        if (existingAccount.HasNoValue)
        {
            var model = new UserAccountModel();
            SetBreadcrumbs(model, pageName);
            return model;
        }

        var accountType = DetermineAccountType(existingAccount.Value);

        var result = new UserAccountModel
        {
            AcceptsTermsAndConditions = new AccountTermsAndConditionsModel
            {
                AcceptsTermsAndConditions = existingAccount.Value?.DateAcceptedTermsAndConditions.HasValue ?? false,
                AcceptsPrivacyPolicy = existingAccount.Value?.DateAcceptedPrivacyPolicy.HasValue ?? false
            },
            UserTypeModel = accountType,
            Status = existingAccount.Value.Status,
            PersonName = new AccountPersonNameModel
            {
                FirstName = existingAccount.Value.FirstName,
                LastName = existingAccount.Value.LastName,
                Title = existingAccount.Value.Title
            },
            PersonContactsDetails = new AccountPersonContactModel
            {
                ContactAddress = new Address
                {
                    Line1 = existingAccount.Value.ContactAddress?.Line1,
                    Line2 = existingAccount.Value.ContactAddress?.Line2,
                    Line3 = existingAccount.Value.ContactAddress?.Line3,
                    Line4 = existingAccount.Value.ContactAddress?.Line4,
                    PostalCode = existingAccount.Value.ContactAddress?.PostalCode
                },
                ContactMobileNumber = existingAccount.Value.ContactMobileTelephone,
                ContactTelephoneNumber = existingAccount.Value.ContactTelephone,
                PreferredContactMethod = existingAccount.Value.PreferredContactMethod
            }
        };
        SetBreadcrumbs(result, pageName);

        return result;
    }

    private static UserTypeModel DetermineAccountType(UserAccount existingAccount)
    {
        AccountType accountType;
        var isOrganisation = false;

        if (existingAccount.IsFcUser())
        {
            accountType = AccountType.FcUser;
            isOrganisation = false;
        }
        else if (existingAccount.IsAgent())
        {
            accountType = AccountType.Agent;
            isOrganisation = existingAccount.Agency?.IsOrganisation is true;
        }
        else
        {
            switch (existingAccount.WoodlandOwner?.WoodlandOwnerType)
            {
                case WoodlandOwnerType.WoodlandOwner:
                    accountType = AccountType.WoodlandOwner;
                    isOrganisation = existingAccount.WoodlandOwner?.IsOrganisation is true;
                    break;
                case WoodlandOwnerType.Tenant:
                    accountType = AccountType.Tenant;
                    isOrganisation = existingAccount.WoodlandOwner?.IsOrganisation is true;
                    break;
                case WoodlandOwnerType.Trust:
                    accountType = AccountType.Trust;
                    isOrganisation = existingAccount.WoodlandOwner?.IsOrganisation is true;
                    break;
                default:
                    accountType = AccountType.WoodlandOwner;
                    break;
            }
        }

        return new UserTypeModel
        {
            AccountType = accountType,
            IsOrganisation = isOrganisation
        };
    }

    private WoodlandOwnerModel GetWoodlandOwnerModel(Maybe<UserAccount> existingAccount)
    {
        if (existingAccount.HasNoValue || existingAccount.Value.WoodlandOwner == null)
        {
            var model = new WoodlandOwnerModel();
            SetBreadcrumbs(model, "Organisation Details");
            return model;
        }

        var result = new WoodlandOwnerModel
        {
            ContactAddress = existingAccount.Value.WoodlandOwner.ContactAddress.IsBlank()
                ? null
                : new Address
                {
                    Line1 = existingAccount.Value.WoodlandOwner.ContactAddress?.Line1,
                    Line2 = existingAccount.Value.WoodlandOwner.ContactAddress?.Line2,
                    Line3 = existingAccount.Value.WoodlandOwner.ContactAddress?.Line3,
                    Line4 = existingAccount.Value.WoodlandOwner.ContactAddress?.Line4,
                    PostalCode = existingAccount.Value.WoodlandOwner.ContactAddress?.PostalCode,
                },
            IsOrganisation = existingAccount.Value.WoodlandOwner?.IsOrganisation ?? false,
            ContactEmail = existingAccount.Value.WoodlandOwner!.ContactEmail,
            ContactName = existingAccount.Value.WoodlandOwner.ContactName,
            OrganisationAddress = existingAccount.Value.WoodlandOwner.OrganisationAddress.IsBlank()
                ? null
                : new Address
                {
                    Line1 = existingAccount.Value.WoodlandOwner.OrganisationAddress?.Line1,
                    Line2 = existingAccount.Value.WoodlandOwner.OrganisationAddress?.Line2,
                    Line3 = existingAccount.Value.WoodlandOwner.OrganisationAddress?.Line3,
                    Line4 = existingAccount.Value.WoodlandOwner.OrganisationAddress?.Line4,
                    PostalCode = existingAccount.Value.WoodlandOwner.OrganisationAddress?.PostalCode
                },
            OrganisationName = existingAccount.Value.WoodlandOwner.OrganisationName,
            ContactTelephoneNumber = existingAccount.Value.WoodlandOwner.ContactTelephone
        };

        result.ContactAddressMatchesOrganisationAddress =
            result.ContactAddress?.Equals(result.OrganisationAddress) ?? false;
        SetBreadcrumbs(result, "Organisation Details");

        return result;
    }

    private AgencyModel GetAgencyModel(Maybe<UserAccount> existingAccount)
    {
        if (existingAccount.HasNoValue || existingAccount.Value.Agency == null)
        {
            var model = new AgencyModel();
            SetBreadcrumbs(model, "Agency Details");
            return model;
        }

        var result = new AgencyModel
        {
            Address = existingAccount.Value.Agency.Address.IsBlank()
                ? null
                : new Address
                {
                    Line1 = existingAccount.Value.Agency.Address?.Line1,
                    Line2 = existingAccount.Value.Agency.Address?.Line2,
                    Line3 = existingAccount.Value.Agency.Address?.Line3,
                    Line4 = existingAccount.Value.Agency.Address?.Line4,
                    PostalCode = existingAccount.Value.Agency.Address?.PostalCode,
                },
            ContactEmail = existingAccount.Value.Agency!.ContactEmail,
            ContactName = existingAccount.Value.Agency.ContactName,
            OrganisationName = existingAccount.Value.Agency.OrganisationName
        };

        SetBreadcrumbs(result, "Agency Details");

        return result;
    }
    
    private void SetBreadcrumbs(PageWithBreadcrumbsViewModel model, string currentPage)
    {
        model.Breadcrumbs = new BreadcrumbsModel
        {
            Breadcrumbs = _breadCrumbsRoot,
            CurrentPage = currentPage
        };
    }
}