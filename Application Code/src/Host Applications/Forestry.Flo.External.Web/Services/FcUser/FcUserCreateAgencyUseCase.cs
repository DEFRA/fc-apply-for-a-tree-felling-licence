using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.Agency;
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

        var request = CreateRequest(model, user);

        try
        {
            var result = await _agencyCreationService.AddAgencyAsync(request, cancellationToken);

            return result.IsSuccess
                ? await HandleSuccess(user, request, result.Value, cancellationToken)
                : await HandleFailure(user, request, result.Error, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught when attempting to create a new agency");
            return await HandleFailure(user, request, ex.Message, cancellationToken);
        }
    }

    private static AddAgencyDetailsRequest CreateRequest(
        FcUserAgencyCreationModel model, 
        ExternalApplicant user)
    {
        return new AddAgencyDetailsRequest
        {
            CreatedByUser = user.UserAccountId!.Value,
            agencyModel = new AgencyModel
            {
                OrganisationName = model.OrganisationName,
                Address = model.Address != null ? ModelMapping.ToAddressEntity(model.Address) : null,
                ContactEmail = model.ContactEmail,
                ContactName = model.ContactName,
                IsFcAgency = false
            }
        };
    }

    private async Task<Result<AddAgencyDetailsResponse>> HandleSuccess(
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
                    request.agencyModel.OrganisationName,
                    request.agencyModel.ContactName
                }),
            cancellationToken);

        _logger.LogDebug("User having account Id of {userId} successfully added a new agency, agency Id is {agencyId} - " +
                         "Agency contact name was {contactName} and organisation name if provided was {OrganisationName}",
            user.UserAccountId, response.AgencyId, request.agencyModel.ContactName, request.agencyModel.OrganisationName);

        return Result.Success(response);
    }

    private async Task<Result<AddAgencyDetailsResponse>> HandleFailure(
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
                    request.agencyModel.OrganisationName,
                    request.agencyModel.ContactName,
                    error
                }),
            cancellationToken);

        _logger.LogDebug("User having account Id of {userId} failed to add a new agency - Agency contact name was {contactName} and organisation name if provided was {OrganisationName}",
            user.UserAccountId, request.agencyModel.ContactName, request.agencyModel.OrganisationName);

        return Result.Failure<AddAgencyDetailsResponse>(error);
    }
}