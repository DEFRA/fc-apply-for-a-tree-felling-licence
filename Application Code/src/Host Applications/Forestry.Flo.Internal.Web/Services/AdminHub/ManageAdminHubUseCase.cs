using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.InternalUsers.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.AdminHubs.Model;
using Forestry.Flo.Services.AdminHubs.Services;
using Forestry.Flo.Internal.Web.Models.AdminHub;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Internal.Web.Services.Interfaces;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;

namespace Forestry.Flo.Internal.Web.Services.AdminHub;

public class ManageAdminHubUseCase : IManageAdminHubUseCase
{
    private readonly RequestContext _requestContext;
    private readonly IAuditService<ManageAdminHubUseCase> _auditService;
    private readonly IAdminHubService _adminHubService;
    private readonly ILogger<ManageAdminHubUseCase> _logger;
    private readonly IUserAccountService _internalUserAccountService;

    public ManageAdminHubUseCase(
        IUserAccountService internalUserAccountService,
        IAdminHubService adminHubService,
        IAuditService<ManageAdminHubUseCase> auditService,
        RequestContext requestContext,
        ILogger<ManageAdminHubUseCase> logger)
    {
        _requestContext = Guard.Against.Null(requestContext);
        _auditService = Guard.Against.Null(auditService);
        _internalUserAccountService = Guard.Against.Null(internalUserAccountService);
        _adminHubService = Guard.Against.Null(adminHubService);
        _logger = logger ?? new NullLogger<ManageAdminHubUseCase>();
    }

    /// <inheritdoc />
    public async Task<Result<ViewAdminHubModel>> RetrieveAdminHubDetailsAsync(
        InternalUser user,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(user);

        _logger.LogDebug("Request received to get admin hubs data");

        var request = new GetAdminHubsDataRequestModel(user.UserAccountId!.Value, user.AccountType!.Value);
        var getAdminHubs = await _adminHubService.RetrieveAdminHubDataAsync(request, cancellationToken);
        if (getAdminHubs.IsFailure)
        {
            _logger.LogError("Could not retrieve admin hubs data with error {Error}", getAdminHubs.Error);
            return Result.Failure<ViewAdminHubModel>("Could not retrieve Admin Hubs");
        }

        var adminHubId = Guid.Empty;
        var adminHubName = "You are not currently a manager at an admin hub";
        string? adminHubAddress = null;
        var managersAdminHub = getAdminHubs.Value.SingleOrDefault(x => x.AdminManagerUserAccountId == user.UserAccountId!.Value);

        if (managersAdminHub != null)
        {
            adminHubId = managersAdminHub.Id;
            adminHubName = managersAdminHub.Name;
            adminHubAddress = managersAdminHub.Address;
        }

        var confirmedUserAccounts = await _internalUserAccountService.ListConfirmedUserAccountsAsync(cancellationToken);
        var adminOfficers = confirmedUserAccounts
            .Where(x => x.AccountType is AccountTypeInternal.AdminOfficer or AccountTypeInternal.AdminHubManager)
            .Select(x => new UserAccountModel { Id = x.Id, FirstName = x.FirstName, LastName = x.LastName, Email = x.Email, AccountType = x.AccountType, Status = x.Status})
            .ToList();

        var model = new ViewAdminHubModel
        {
            Id = adminHubId,
            Name = adminHubName,
            Address = adminHubAddress,
            AdminHubs = getAdminHubs.Value,
            AllAdminOfficers = adminOfficers
        };

        return Result.Success(model);

    }

    /// <inheritdoc />
    public async Task<UnitResult<ManageAdminHubOutcome>> AddAdminOfficerAsync(
        ViewAdminHubModel model,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(user);
        Guard.Against.Null(model);

        _logger.LogDebug("Request received to add admin officer with id {AdminOfficerId} to admin hub with id {AdminHubId}", model.SelectedOfficerId.Value, model.Id);

        var requestModel = new AddAdminOfficerToAdminHubRequestModel(
            model.Id, model.SelectedOfficerId!.Value, user.AccountType!.Value, user.UserAccountId!.Value);

        var result = await _adminHubService.AddAdminOfficerAsync(requestModel, cancellationToken);

        if (result.IsSuccess)
        {
            await AuditOutcome(AuditEvents.AddAdminOfficerToAdminHub, model.Id, user.UserAccountId!.Value,
                new { AdminOfficerId = model.SelectedOfficerId.Value }, cancellationToken);
            return result;
        }

        _logger.LogError("Could not add admin officer with id {AdminOfficerId} to admin hub with id {AuditHubId} with error {Error}",
            model.SelectedOfficerId, model.Id, result.Error);

        if (result.Error != ManageAdminHubOutcome.NoChangeSubmitted)
        {
            await AuditOutcome(AuditEvents.AddAdminOfficerToAdminHubFailure, model.Id, user.UserAccountId!.Value,
                new { AdminOfficerId = model.SelectedOfficerId, Error = result.Error }, cancellationToken);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<UnitResult<ManageAdminHubOutcome>> RemoveAdminOfficerAsync(
        ViewAdminHubModel model,
        InternalUser user,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(user);
        Guard.Against.Null(model);

        _logger.LogDebug("Request received to remove admin officer with id {AdminOfficerId} from admin hub with id {AdminHubId}", model.SelectedOfficerId.Value, model.Id);

        var requestModel = new RemoveAdminOfficerFromAdminHubRequestModel(
            model.Id, model.SelectedOfficerId!.Value, user.AccountType!.Value, user.UserAccountId!.Value);

        var result = await _adminHubService.RemoveAdminOfficerAsync(requestModel, cancellationToken);

        if (result.IsSuccess)
        {
            await AuditOutcome(AuditEvents.RemoveAdminOfficerFromAdminHub, model.Id, user.UserAccountId!.Value,
                new { AdminOfficerId = model.SelectedOfficerId.Value }, cancellationToken);
            return result;
        }

        _logger.LogError("Could not remove admin officer with id {AdminOfficerId} from admin hub with id {AuditHubId} with error {Error}",
            model.SelectedOfficerId, model.Id, result.Error);

        if (result.Error != ManageAdminHubOutcome.NoChangeSubmitted)
        {
            await AuditOutcome(AuditEvents.RemoveAdminOfficerFromAdminHubFailure, model.Id, user.UserAccountId!.Value,
                new { AdminOfficerId = model.SelectedOfficerId, Error = result.Error }, cancellationToken);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<UnitResult<ManageAdminHubOutcome>> EditAdminHub(
        ViewAdminHubModel model, 
        InternalUser user, 
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(user);
        Guard.Against.Null(model);

        _logger.LogDebug("Request received to update admin hub with id {AdminHubId}", model.Id);

        var requestModel = new UpdateAdminHubDetailsRequestModel(
            model.Id, 
            model.SelectedOfficerId!.Value, 
            model.Name!,
            model.Address!,
            user.UserAccountId!.Value, 
            user.AccountType!.Value);

        var result = await _adminHubService.UpdateAdminHubDetailsAsync(requestModel, cancellationToken);

        if (result.IsSuccess)
        {
            await AuditOutcome(AuditEvents.UpdateAdminHubDetails, model.Id, user.UserAccountId!.Value,
                new { Name = model.Name, AdminManagerId = model.SelectedOfficerId.Value }, cancellationToken);
            return result;
        }

        _logger.LogError("Could not update admin hub with id {AuditHubId} with error {Error}", model.Id, result.Error);

        if (result.Error != ManageAdminHubOutcome.NoChangeSubmitted)
        {
            await AuditOutcome(AuditEvents.UpdateAdminHubDetailsFailure, model.Id, user.UserAccountId!.Value,
                new { Name = model.Name, AdminManagerId = model.SelectedOfficerId, Error = result.Error }, cancellationToken);
        }

        return result;
    }

    private async Task AuditOutcome(string eventName, Guid adminHubId, Guid performingUserId, object? auditData, CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            eventName,
            adminHubId,
            performingUserId,
            _requestContext,
            auditData), cancellationToken);
    }
}