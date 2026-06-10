using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.Agency;
using Forestry.Flo.External.Web.Models.UserAccount.AccountTypeViewModels;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using AgencyModel = Forestry.Flo.Services.Applicants.Models.AgencyModel;

namespace Forestry.Flo.External.Web.Services.FcUser;

/// <summary>
/// Class orchestrating the required co-ordination to create a new Agency.
/// </summary>
public class FcUserCreateAgencyUseCase
{
    private readonly IAgencyCreationService _agencyCreationService;
    private readonly IAuditService<FcUserCreateAgencyUseCase> _auditService;
    private readonly RequestContext _requestContext;
    private readonly ILogger<FcUserCreateAgencyUseCase> _logger;

    public FcUserCreateAgencyUseCase(
        IAgencyCreationService agencyCreationService,
        IAuditService<FcUserCreateAgencyUseCase> auditService,
        RequestContext requestContext,
        ILogger<FcUserCreateAgencyUseCase> logger)
    {
        _agencyCreationService = Guard.Against.Null(agencyCreationService);
        _auditService = Guard.Against.Null(auditService);
        _requestContext = requestContext;
        _logger = logger;
    }

    /// <summary>
    /// Create a new agency with values from the view model.
    /// </summary>
    /// <param name="user">An <see cref="ExternalApplicant"/> representing the current user.</param>
    /// <param name="model">The model containing the details of the agency to add to the system.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Success result containing the Id of the new agency, or the failure of the operation with the error reason.</returns>
    public async Task<Result<AddAgencyDetailsResponse>> ExecuteAsync(
        ExternalApplicant user,
        FcUserAgencyCreationModel model,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(user);
        Guard.Against.Null(model);

        if (user.IsFcUser == false)
        {
            _logger.LogWarning("User having account id {userId} has attempted to create a new agency, but it was blocked as user is not an FC user!", user.UserAccountId);
            return Result.Failure<AddAgencyDetailsResponse>("The current user is not permitted to perform this action.");
        }

        if (model.AgencyId.HasValue)
        {
            var updateRequest = CreateUpdateRequest(model, user);

            try
            {
                var updateResult = await _agencyCreationService.UpdateAgencyAsync(updateRequest, cancellationToken);

                return updateResult.IsSuccess
                    ? await HandleUpdateSuccess(user, updateRequest.AgencyId, updateRequest.AgencyModel, cancellationToken)
                    : await HandleUpdateFailure(user, updateRequest.AgencyId, updateRequest.AgencyModel, updateResult.Error, cancellationToken);
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Exception caught when attempting to create a new agency");
                return await HandleUpdateFailure(user, model.AgencyId!.Value, updateRequest.AgencyModel, updateEx.Message, cancellationToken);
            }
        }

        var addRequest = CreateAddRequest(model, user);

        try
        {
            var addResult = await _agencyCreationService.AddAgencyAsync(addRequest, cancellationToken);

            return addResult.IsSuccess
                ? await HandleAddSuccess(user, addRequest, addResult.Value, cancellationToken)
                : await HandleAddFailure(user, addRequest, addResult.Error, cancellationToken);
        }
        catch (Exception addEx)
        {
            _logger.LogError(addEx, "Exception caught when attempting to create a new agency");
            return await HandleAddFailure(user, addRequest, addEx.Message, cancellationToken);
        }
    }

    public async Task<Result<FcUserAgencyCreationModel>> GetExistingAgencyDetailsForEditAsync(
        ExternalApplicant user,
        Guid agencyId,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(user);
        if (user.IsFcUser == false)
        {
            _logger.LogWarning("User having account id {userId} has attempted to edit an agency, but it was blocked as user is not an FC user!", user.UserAccountId);
            return Result.Failure<FcUserAgencyCreationModel>("The current user is not permitted to perform this action.");
        }
        var getResult = await _agencyCreationService.GetAgencyDetailsAsync(agencyId, cancellationToken);
        if (getResult.IsFailure)
        {
            _logger.LogError("Failed to retrieve agency details for agency {agencyId} for editing, error was {error}", agencyId, getResult.Error);
            return Result.Failure<FcUserAgencyCreationModel>("Failed to retrieve existing agency details");
        }
        var agency = getResult.Value;
        var model = new FcUserAgencyCreationModel
        {
            AgencyId = agencyId,
            IsOrganisation = agency.IsOrganisation,
            OrganisationStatus = agency.IsOrganisation ? OrganisationStatus.Organisation : OrganisationStatus.Individual,
            OrganisationName = agency.OrganisationName,
            ContactEmail = agency.ContactEmail,
            ContactName = agency.ContactName,
            Address = agency.Address != null ? ModelMapping.ToAddressModel(agency.Address) : null
        };
        return Result.Success(model);
    }

    private static AddAgencyDetailsRequest CreateAddRequest(
        FcUserAgencyCreationModel model, 
        ExternalApplicant user)
    {
        return new AddAgencyDetailsRequest
        {
            CreatedByUser = user.UserAccountId!.Value,
            AgencyModel = new AgencyModel
            {
                IsOrganisation = model.IsOrganisation,
                OrganisationName = model.OrganisationName,
                Address = model.Address != null ? ModelMapping.ToAddressEntity(model.Address) : null,
                ContactEmail = model.ContactEmail,
                ContactName = model.ContactName,
                IsFcAgency = false
            }
        };
    }

    private static UpdateAgencyDetailsRequest CreateUpdateRequest(
        FcUserAgencyCreationModel model,
        ExternalApplicant user)
    {
        return new UpdateAgencyDetailsRequest
        {
            AgencyId = model.AgencyId!.Value,
            UpdatedByUser = user.UserAccountId!.Value,
            AgencyModel = new AgencyModel
            {
                IsOrganisation = model.IsOrganisation,
                OrganisationName = model.OrganisationName,
                Address = model.Address != null ? ModelMapping.ToAddressEntity(model.Address) : null,
                ContactEmail = model.ContactEmail,
                ContactName = model.ContactName,
                IsFcAgency = false
            }
        };
    }

    private async Task<Result<AddAgencyDetailsResponse>> HandleAddSuccess(
        ExternalApplicant user,
        AddAgencyDetailsRequest request,
        AddAgencyDetailsResponse response,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.FcUserCreateAgencyEvent,
                response.AgencyId,
                user.UserAccountId,
                _requestContext,
                new
                {
                    response.AgencyId,
                    request.AgencyModel.OrganisationName,
                    request.AgencyModel.ContactName
                }),
            cancellationToken);

        _logger.LogDebug("User having account Id of {userId} successfully added a new agency, agency Id is {agencyId} - " +
                         "Agency contact name was {contactName} and organisation name if provided was {OrganisationName}",
            user.UserAccountId, response.AgencyId, request.AgencyModel.ContactName, request.AgencyModel.OrganisationName);

        return Result.Success(response);
    }

    private async Task<Result<AddAgencyDetailsResponse>> HandleUpdateSuccess(
        ExternalApplicant user,
        Guid agencyId,
        AgencyModel requestModel,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.FcUserUpdateAgencyEvent,
                agencyId,
                user.UserAccountId,
                _requestContext,
                new
                {
                    requestModel.OrganisationName,
                    requestModel.ContactName
                }),
            cancellationToken);

        _logger.LogDebug("User having account Id of {userId} successfully updated and existing agency, agency Id is {agencyId} - " +
                         "Agency contact name was {contactName} and organisation name if provided was {OrganisationName}",
            user.UserAccountId, agencyId, requestModel.ContactName, requestModel.OrganisationName);

        return Result.Success(new AddAgencyDetailsResponse{AgencyId = agencyId});
    }

    private async Task<Result<AddAgencyDetailsResponse>> HandleAddFailure(
        ExternalApplicant user,
        AddAgencyDetailsRequest request,
        string error,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.FcUserCreateAgencyFailureEvent,
                null,
                user.UserAccountId,
                _requestContext,
                new
                {
                    request.AgencyModel.OrganisationName,
                    request.AgencyModel.ContactName,
                    error
                }),
            cancellationToken);

        _logger.LogDebug("User having account Id of {userId} failed to add a new agency - Agency contact name was {contactName} and organisation name if provided was {OrganisationName}",
            user.UserAccountId, request.AgencyModel.ContactName, request.AgencyModel.OrganisationName);

        return Result.Failure<AddAgencyDetailsResponse>(error);
    }

    private async Task<Result<AddAgencyDetailsResponse>> HandleUpdateFailure(
        ExternalApplicant user,
        Guid agencyId,
        AgencyModel requestModel,
        string error,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.FcUserUpdateAgencyFailureEvent,
                agencyId,
                user.UserAccountId,
                _requestContext,
                new
                {
                    requestModel.OrganisationName,
                    requestModel.ContactName,
                    error
                }),
            cancellationToken);

        _logger.LogDebug("User having account Id of {userId} failed to update and existing agency {AgencyId} - Agency contact name was {contactName} and organisation name if provided was {OrganisationName}",
            user.UserAccountId, agencyId, requestModel.ContactName, requestModel.OrganisationName);

        return Result.Failure<AddAgencyDetailsResponse>(error);
    }
}