using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using WoodlandOwnerModel = Forestry.Flo.External.Web.Models.WoodlandOwner.WoodlandOwnerModel;

namespace Forestry.Flo.External.Web.Services;

public class FcAgentCreatesWoodlandOwnerUseCase
{
    private readonly IWoodlandOwnerCreationService _woodlandOwnerCreationService;
    private readonly RequestContext _requestContext;
    private readonly IAuditService<FcAgentCreatesWoodlandOwnerUseCase> _auditService;
    private readonly ILogger<FcAgentCreatesWoodlandOwnerUseCase> _logger;

    public FcAgentCreatesWoodlandOwnerUseCase(
        IWoodlandOwnerCreationService woodlandOwnerCreationService,
        IAuditService<FcAgentCreatesWoodlandOwnerUseCase> auditService,
        RequestContext requestContext,
        ILogger<FcAgentCreatesWoodlandOwnerUseCase> logger
        )
    {
        _woodlandOwnerCreationService = Guard.Against.Null(woodlandOwnerCreationService);
        _auditService = Guard.Against.Null(auditService);
        _requestContext = requestContext;
        _logger = logger;
    }

    /// <summary>
    /// Create a new woodland owner with values from the view model.
    /// </summary>
    /// <param name="user">An <see cref="ExternalApplicant"/> representing the current user.</param>
    /// <param name="model">The model containing the details of the woodland owner to add to the system.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>Success or failure of the operation.</returns>
    public async Task<Result<AddWoodlandOwnerDetailsResponse>> CreateWoodlandOwnerAsync(
        ExternalApplicant user, 
        WoodlandOwnerModel model, 
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(user);
        Guard.Against.Null(model);

        if (user.IsFcUser == false)
        {
            _logger.LogWarning("User having account id [{userId}] has attempted to create a new Woodland Owner, but it was blocked as user does not have the required claim!", user.UserAccountId);
            return Result.Failure<AddWoodlandOwnerDetailsResponse>("The current user is not permitted to perform this action.");
        }

        if (user.HasRegisteredLocalAccount == false)
        {
            return Result.Failure<AddWoodlandOwnerDetailsResponse>("There is no local user account for the current user.");
        }

        var request = CreateRequest(model, user);

        try
        {
            var result = await _woodlandOwnerCreationService.AddWoodlandOwnerDetails(request, cancellationToken);

            return result.IsSuccess
                ? await HandleSuccess(user, request, result.Value, cancellationToken)
                : await HandleFailure(user, request, result.Error, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception caught when attempting to create a Woodland Owner by an FC Agent.");
            return await HandleFailure(user, request, ex.Message, cancellationToken);
        }
    }

    private static AddWoodlandOwnerDetailsRequest CreateRequest(WoodlandOwnerModel model, ExternalApplicant user)
    {
        return new AddWoodlandOwnerDetailsRequest
        {
            CreatedByUser = user.UserAccountId!.Value,
            WoodlandOwner = new Flo.Services.Applicants.Models.WoodlandOwnerModel
            {
                OrganisationName = model.OrganisationName,
                OrganisationAddress = model.OrganisationAddress!=null ? ModelMapping.ToAddressEntity(model.OrganisationAddress) : null,
                ContactAddress = ModelMapping.ToAddressEntity(model.ContactAddress!),
                ContactTelephone = model.ContactTelephoneNumber,
                ContactEmail = model.ContactEmail,
                ContactName = model.ContactName,
                IsOrganisation = model.IsOrganisation
            }
        };
    }

    private async Task<Result<AddWoodlandOwnerDetailsResponse>> HandleSuccess(
        ExternalApplicant user, 
        AddWoodlandOwnerDetailsRequest request,
        AddWoodlandOwnerDetailsResponse response,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.FcAgentUserCreateWoodlandOwnerEvent, 
                response.WoodlandOwnerId,
                user.UserAccountId,
                _requestContext,
                new
                {
                    response.WoodlandOwnerId,
                    request.WoodlandOwner.ContactName,
                    request.WoodlandOwner.ContactEmail,
                    request.WoodlandOwner.ContactTelephone,
                    request.WoodlandOwner.ContactAddress,
                    request.WoodlandOwner.IsOrganisation,
                    request.WoodlandOwner.OrganisationName,
                    request.WoodlandOwner.OrganisationAddress
                }),
            cancellationToken);

        _logger.LogDebug("User having account Id of [{userId}] successfully added a new Woodland Owner, organisation name is [{organisationName}], and id is [{WoodlandOwnerId}].",
            user.UserAccountId,
            request.WoodlandOwner.OrganisationName,
            response.WoodlandOwnerId);

        return Result.Success(response);
    }

    private async Task<Result<AddWoodlandOwnerDetailsResponse>> HandleFailure(
        ExternalApplicant user, 
        AddWoodlandOwnerDetailsRequest request, 
        string error, 
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.FcAgentUserCreateWoodlandOwnerFailureEvent, 
                null,
                user.UserAccountId,
                _requestContext,
                new
                {
                    request.WoodlandOwner.ContactName,
                    request.WoodlandOwner.ContactEmail,
                    request.WoodlandOwner.ContactTelephone,
                    request.WoodlandOwner.ContactAddress,
                    request.WoodlandOwner.IsOrganisation,
                    request.WoodlandOwner.OrganisationName,
                    request.WoodlandOwner.OrganisationAddress,
                    error
                }
            ),
            cancellationToken);
        
        _logger.LogDebug("User having account Id of [{userId}] failed to add a new Woodland Owner detail - Organisation Name was [{OrganisationName}].",
            user.UserAccountId,
            request.WoodlandOwner.OrganisationName);

        return Result.Failure<AddWoodlandOwnerDetailsResponse>(error);
    }
}