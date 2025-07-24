using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.UserAccount;
using Forestry.Flo.External.Web.Models.UserAccount.AccountTypeViewModels;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.Applicants.Services;
using Microsoft.AspNetCore.Mvc;
using TenantType = Forestry.Flo.External.Web.Models.UserAccount.AccountTypeViewModels.TenantType;

namespace Forestry.Flo.External.Web.Controllers;

public partial class AccountController
{
    private const string AccountTypeErrorMessage = "Unable to update your account type";

    [HttpGet]
    public async Task<IActionResult> AgentTypeSelection(
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var existingAccount = await useCase.RetrieveExistingAccountAsync(user, cancellationToken);

        var model = new AgentTypeViewModel
        {
            OrganisationStatus = existingAccount.HasValue && existingAccount.Value.IsAgent()
                ? existingAccount.Value.Agency?.IsOrganisation is true
                    ? OrganisationStatus.Organisation
                    : OrganisationStatus.Individual
                : null,
        };

        SetBreadcrumbs(model, "Agent Type");
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> AgentTypeSelection(
        AgentTypeViewModel model,
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            SetBreadcrumbs(model, "Agent Type");
            return View(model);
        }
        
        var userTypeModel = new UserTypeModel
        {
            AccountType = AccountType.Agent,
            IsOrganisation = model.OrganisationStatus is OrganisationStatus.Organisation
        };

        var accountTypeResult = await SetOrUpdateAccountType(
            userTypeModel,
            useCase,
            cancellationToken);

        if (userTypeModel.IsOrganisation)
        {
            return RedirectToAction("RegisterAgencyDetails");
        }

        await useCase.RevertAgencyOrganisationToIndividualAsync(new ExternalApplicant(User), cancellationToken);

        var user = new ExternalApplicant(User);

        return RedirectToAction(
            user.HasCompletedAccountRegistration ?
            nameof(AccountSummary) :
            nameof(RegisterPersonName));
    }

    [HttpGet]
    public async Task<IActionResult> OwnerTypeSelection(
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var existingAccount = await useCase.RetrieveExistingAccountAsync(user, cancellationToken);

        var model = new OwnerTypeViewModel
        {
            OrganisationStatus = existingAccount.HasValue && existingAccount.Value.WoodlandOwner?.IsOrganisation is not null
                ? existingAccount.Value.WoodlandOwner!.IsOrganisation
                    ? OrganisationStatus.Organisation
                    : OrganisationStatus.Individual
                : null,
        };

        SetBreadcrumbs(model, "Owner Type");
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> OwnerTypeSelection(
        OwnerTypeViewModel model,
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            SetBreadcrumbs(model, "Owner Type");
            return View(model);
        }

        var userTypeModel = new UserTypeModel
        {
            AccountType = AccountType.WoodlandOwner,
            IsOrganisation = model.OrganisationStatus is OrganisationStatus.Organisation
        };

        var accountTypeResult = await SetOrUpdateAccountType(
            userTypeModel,
            useCase,
            cancellationToken);

        if (accountTypeResult.IsFailure)
        {
            this.AddErrorMessage(AccountTypeErrorMessage);
        }

        return userTypeModel.IsOrganisation
            ? RedirectToAction("RegisterOrganisationDetails")
            : RedirectToAction("RegisterPersonName");
    }

    [HttpGet]
    public async Task<IActionResult> TrustTypeSelection(
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var existingAccount = await useCase.RetrieveExistingAccountAsync(user, cancellationToken);

        var model = new TrustTypeViewModel
        {
            OrganisationStatus = existingAccount.HasValue 
                                 && existingAccount.Value.WoodlandOwner?.IsOrganisation != null
                ? existingAccount.Value.WoodlandOwner!.IsOrganisation
                    ? OrganisationStatus.Organisation
                    : OrganisationStatus.Individual
                : null,
        };

        SetBreadcrumbs(model, "Trust Type");
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> TrustTypeSelection(
        TrustTypeViewModel model,
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            SetBreadcrumbs(model, "Trust Type");
            return View(model);
        }

        var userTypeModel = new UserTypeModel
        {
            AccountType = AccountType.Trust,
            IsOrganisation = model.OrganisationStatus is OrganisationStatus.Organisation
        };

        var accountTypeResult = await SetOrUpdateAccountType(
            userTypeModel,
            useCase,
            cancellationToken);

        if (accountTypeResult.IsFailure)
        {
            this.AddErrorMessage(AccountTypeErrorMessage);
        }

        return userTypeModel.IsOrganisation
            ? RedirectToAction("RegisterOrganisationDetails")
            : RedirectToAction("RegisterPersonName");
    }

    [HttpGet]
    public async Task<IActionResult> TenantTypeSelection(
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var existingAccount = await useCase.RetrieveExistingAccountAsync(user, cancellationToken);

        var model = new TenantTypeViewModel
        {
            TenantType = existingAccount.HasValue && existingAccount.Value.WoodlandOwner?.TenantType is not null
                ? ModelMapping.ToTenantType(existingAccount.Value.WoodlandOwner.TenantType)
                : null,
        };

        SetBreadcrumbs(model, "Tenant Type");
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> TenantTypeSelection(
        TenantTypeViewModel model,
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            SetBreadcrumbs(model, "Tenant Type");
            return View(model);
        }

        if (model.TenantType is TenantType.CrownLand)
        {
            return RedirectToAction(nameof(LandlordDetails));
        }

        var accountTypeResult = await SetOrUpdateAccountType(
            new UserTypeModel
            {
                AccountType = AccountType.Tenant
            },
            useCase,
            cancellationToken);

        if (accountTypeResult.IsFailure)
        {
            this.AddErrorMessage(AccountTypeErrorMessage);
        }

        var user = new ExternalApplicant(User);

        return RedirectToAction(
            user.HasCompletedAccountRegistration ?
            nameof(AccountSummary) :
            nameof(TenantOrgOrIndividualSelection));
    }

    [HttpGet]
    public async Task<IActionResult> TenantOrgOrIndividualSelection(
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var existingAccount = await useCase.RetrieveExistingAccountAsync(user, cancellationToken);

        var model = new TenantIndividualOrOrganisationViewModel
        {
            OrganisationStatus = existingAccount.HasValue
                                 && existingAccount.Value.WoodlandOwner?.IsOrganisation != null
                ? existingAccount.Value.WoodlandOwner!.IsOrganisation
                    ? OrganisationStatus.Organisation
                    : OrganisationStatus.Individual
                : null,
        };

        SetBreadcrumbs(model, "Individual or Organisation Tenant");
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> TenantOrgOrIndividualSelection(
        TenantIndividualOrOrganisationViewModel model,
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            SetBreadcrumbs(model, "Individual or Organisation Tenant");
            return View(model);
        }

        var user = new ExternalApplicant(User);
        var existingAccount = await useCase.RetrieveExistingAccountAsync(user, cancellationToken);

        LandlordDetails? landlordDetails = null;
        if (existingAccount.HasValue && existingAccount.Value.WoodlandOwner?.TenantType is Flo.Services.Applicants.Entities.WoodlandOwner.TenantType.CrownLand)
        {
            landlordDetails = new LandlordDetails(
                existingAccount.Value.WoodlandOwner!.LandlordFirstName,
                existingAccount.Value.WoodlandOwner!.LandlordLastName);
        }

        var userTypeModel = new UserTypeModel
        {
            AccountType = AccountType.Tenant,
            IsOrganisation = model.OrganisationStatus is OrganisationStatus.Organisation
        };

        var accountTypeResult = await SetOrUpdateAccountType(
            userTypeModel,
            useCase,
            cancellationToken,
            landlordDetails);

        if (accountTypeResult.IsFailure)
        {
            this.AddErrorMessage(AccountTypeErrorMessage);
        }

        return userTypeModel.IsOrganisation
            ? RedirectToAction("RegisterOrganisationDetails")
            : RedirectToAction("RegisterPersonName");
    }

    [HttpGet]
    public async Task<IActionResult> LandlordDetails(
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        var user = new ExternalApplicant(User);
        var existingAccount = await useCase.RetrieveExistingAccountAsync(user, cancellationToken);

        var model = new LandlordDetailsViewModel
        {
            FirstName = existingAccount.HasValue && existingAccount.Value.WoodlandOwner?.LandlordFirstName is not null
                ? existingAccount.Value.WoodlandOwner?.LandlordFirstName
                : null,
            LastName = existingAccount.HasValue && existingAccount.Value.WoodlandOwner?.LandlordLastName is not null
                ? existingAccount.Value.WoodlandOwner?.LandlordLastName
                : null
        };

        SetBreadcrumbs(model, "Landlord Details");
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> LandlordDetails(
        LandlordDetailsViewModel model,
        [FromServices] RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            SetBreadcrumbs(model, "Landlord Details");
            return View(model);
        }
        
        var accountTypeResult = await SetOrUpdateAccountType(
            new UserTypeModel
            {
                AccountType = AccountType.Tenant
            },
            useCase,
            cancellationToken,
            new LandlordDetails(model.FirstName!, model.LastName!));

        if (accountTypeResult.IsFailure)
        {
            this.AddErrorMessage(AccountTypeErrorMessage);
        }
        var user = new ExternalApplicant(User);

        return RedirectToAction(
            user.HasCompletedAccountRegistration ?
            nameof(AccountSummary) :
            nameof(TenantOrgOrIndividualSelection));
    }

    private async Task<Result> SetOrUpdateAccountType(
        UserTypeModel userTypeModel,
        RegisterUserAccountUseCase useCase,
        CancellationToken cancellationToken,
        LandlordDetails? landlordDetails = null)
    {
        var user = new ExternalApplicant(User);
        if (!user.HasCompletedAccountRegistration)
        {
            var existingAccount = await useCase.RetrieveExistingAccountAsync(user, cancellationToken);

            if (existingAccount.HasValue)
            {
                return await useCase.UpdateAccountTypeAsync(user, userTypeModel, landlordDetails, cancellationToken);
            }

            if (existingAccount.HasNoValue)
            {
                return await useCase.RegisterNewAccountAsync(user, userTypeModel, landlordDetails, cancellationToken);
            }
        }
        else
        {
            _logger.LogWarning("Attempt to register account type for an account that has already been registered");
            return Result.Failure("Cannot update account type for account that has already been registered");
        }

        return Result.Success();
    }
}