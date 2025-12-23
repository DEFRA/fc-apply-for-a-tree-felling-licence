using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Services.Interfaces;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.MassTransit.Messages;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.External.Web.Services;

/// <summary>
/// Use case for checking if any compartments within a Felling Licence Application intersects
/// PAWS areas and require extra data input from the applicant.
/// </summary>
public class CheckForPawsRequirementUseCase(
    RequestContext requestContext,
    IGetFellingLicenceApplicationForExternalUsers getFellingLicenceApplicationService,
    IGetPropertyProfiles getPropertyProfilesService,
    IUpdateApplicationFromForesterLayers updateApplicationFromForesterLayers,
    IAuditService<CheckForPawsRequirementUseCase> auditService,
    ILogger<CheckForPawsRequirementUseCase> logger) : ICheckForPawsRequirementUseCase
{
    private readonly RequestContext _requestContext = Guard.Against.Null(requestContext);
    private readonly IGetFellingLicenceApplicationForExternalUsers _getFellingLicenceApplicationService = Guard.Against.Null(getFellingLicenceApplicationService);
    private readonly IGetPropertyProfiles _getPropertyProfilesService = Guard.Against.Null(getPropertyProfilesService);
    private readonly IUpdateApplicationFromForesterLayers _updateApplicationFromForesterLayers = Guard.Against.Null(updateApplicationFromForesterLayers);
    private readonly IAuditService<CheckForPawsRequirementUseCase> _auditService = Guard.Against.Null(auditService);
    private readonly ILogger<CheckForPawsRequirementUseCase> _logger = logger ?? new NullLogger<CheckForPawsRequirementUseCase>();

    /// <inheritdoc />
    public async Task<Result> CheckAndUpdateApplicationForPaws(
        PawsRequirementCheckMessage message,
        CancellationToken cancellationToken)
    {
        var userAccessModel = new UserAccessModel
        {
            IsFcUser = message.IsFcUser,
            AgencyId = message.AgencyId,
            UserAccountId = message.UserId,
            WoodlandOwnerIds = [message.WoodlandOwnerId]
        };

        // retrieve the application to be processed

        var (_, applicationRetrievalFailure, application, error) = await _getFellingLicenceApplicationService.GetApplicationByIdAsync(
            message.ApplicationId,
            userAccessModel,
            cancellationToken);

        if (applicationRetrievalFailure)
        {
            var errorMessage = $"Failed to retrieve application with id {message.ApplicationId} to check compartments for PAWS: {error}";
            await AuditFailure(message, errorMessage, cancellationToken);
            return Result.Failure(errorMessage);
        }

        // get the property profile linked to the application

        var (_, propertyRetrievealFailure, propertyProfile, propertyError) = await _getPropertyProfilesService.GetPropertyByIdAsync(
            application.LinkedPropertyProfile.PropertyProfileId,
            userAccessModel,
            cancellationToken);

        if (propertyRetrievealFailure)
        {
            var errorMessage = $"Failed to retrieve property with id {application.LinkedPropertyProfile?.PropertyProfileId} on application with id {message.ApplicationId} to check compartments for PAWS: {propertyError}";
            await AuditFailure(message, errorMessage, cancellationToken);
            return Result.Failure(errorMessage);
        }

        // get the compartments on the property that are included in the application

        var selectedCompartmentIds = application.GetAllCompartmentIdsInApplication();

        var relevantCompartments = propertyProfile.Compartments
            .Where(c => selectedCompartmentIds.Contains(c.Id))
            .ToList();

        if (relevantCompartments.Count != selectedCompartmentIds.Count()
            || relevantCompartments.Any(c => c.GISData == null))
        {
            var errorMessage = $"Failed to retrieve all selected compartments or GIS data is missing for application with id {message.ApplicationId} to check compartments for PAWS.";
            await AuditFailure(message, errorMessage, cancellationToken);
            return Result.Failure(errorMessage);
        }

        var compartmentGisData = relevantCompartments
            .Select(c => new { c.Id, c.GISData })
            .ToDictionary(t => t.Id, t => t.GISData);

        // update the application based on whether any compartments intersect PAWS areas

        var updateResult = await _updateApplicationFromForesterLayers.UpdateForPawsLayersAsync(
            message.ApplicationId, compartmentGisData!, cancellationToken);

        if (updateResult.IsFailure)
        {
            var errorMessage = $"Failed to update application with id {message.ApplicationId} after checking compartments for PAWS: {updateResult.Error}";
            await AuditFailure(message, errorMessage, cancellationToken);
            return Result.Failure(errorMessage);
        }

        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.PawsRequirementCheckCompleted,
                message.ApplicationId,
                message.UserId,
                _requestContext,
                new
                {
                    WoodlandOwner = message.WoodlandOwnerId,
                }),
            cancellationToken);

        return Result.Success();
    }

    private async Task AuditFailure(
        PawsRequirementCheckMessage message,
        string error,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.PawsRequirementCheckFailed,
                message.ApplicationId,
                message.UserId,
                _requestContext,
                new
                {
                    WoodlandOwner = message.WoodlandOwnerId,
                    Error = error
                }),
            cancellationToken);
    }
}