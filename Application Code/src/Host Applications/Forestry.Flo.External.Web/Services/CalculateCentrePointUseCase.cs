using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.MassTransit.Messages;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.External.Web.Services;

public class CalculateCentrePointUseCase
{
    private readonly IAuditService<CalculateCentrePointUseCase> _auditService;
    private readonly IGetFellingLicenceApplicationForExternalUsers _getFellingLicenceApplicationService;
    private readonly IUpdateCentrePoint _updateCentrePoint;
    private readonly ILogger<CalculateCentrePointUseCase> _logger;
    private readonly RequestContext _requestContext;
    private readonly ISubmitFellingLicenceService _submitFellingLicenceService;
    private readonly IPropertyProfileRepository _propertyProfileRepository;

    public CalculateCentrePointUseCase(
        IAuditService<CalculateCentrePointUseCase> auditService,
        RequestContext requestContext,
        ISubmitFellingLicenceService submitFellingLicenceService,
        IGetFellingLicenceApplicationForExternalUsers getFellingLicenceApplicationService,
        IUpdateCentrePoint updateCentrePoint,
        ILogger<CalculateCentrePointUseCase> logger,
        IOptions<InternalUserSiteOptions> internalUserSiteOptions,
        IPropertyProfileRepository propertyProfileRepository)
    {
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(requestContext);
        ArgumentNullException.ThrowIfNull(submitFellingLicenceService);
        ArgumentNullException.ThrowIfNull(getFellingLicenceApplicationService);
        ArgumentNullException.ThrowIfNull(updateCentrePoint);
        ArgumentNullException.ThrowIfNull(internalUserSiteOptions);
        ArgumentNullException.ThrowIfNull(propertyProfileRepository);

        _auditService = auditService;
        _requestContext = requestContext;
        _submitFellingLicenceService = submitFellingLicenceService;
        _getFellingLicenceApplicationService = getFellingLicenceApplicationService;
        _updateCentrePoint = updateCentrePoint;
        _logger = logger;
        _propertyProfileRepository = propertyProfileRepository;
    }

    /// <summary>
    /// Calculates the centre point and OS grid reference for a given application, and attempts to use these values to assign a woodland officer.
    /// </summary>
    /// <param name="message">A populated <see cref="CentrePointCalculationMessage"/> containing data to calculate these values with.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating whether the centre point and OS grid references for an application have been updated.</returns>
    public async Task<Result> CalculateCentrePointAsync(
        CentrePointCalculationMessage message,
        CancellationToken cancellationToken)
    {
        string errorMessage;

        var userAccModel = new UserAccessModel
        {
            IsFcUser = message.IsFcUser,
            AgencyId = message.AgencyId,
            UserAccountId = message.UserId,
            WoodlandOwnerIds = new List<Guid> { message.WoodlandOwnerId }
        };

        var (_, applicationRetrievalFailure, application) =
            await _getFellingLicenceApplicationService.GetApplicationByIdAsync(message.ApplicationId, userAccModel, cancellationToken);

        if (applicationRetrievalFailure)
        {
            errorMessage = $"Unable to retrieve application with identifier {message.ApplicationId}";
            await PublishCentrePointFailures(message, errorMessage, cancellationToken);

            return Result.Failure(errorMessage);
        }
        
        // Get property profile via linked property profile table

        var selectedCompartmentIds = application.LinkedPropertyProfile?.ProposedFellingDetails?
                .Select(d => d.PropertyProfileCompartmentId).ToList();

        var linkedPropertyProfile = application.LinkedPropertyProfile;

        var propertyProfile = await _propertyProfileRepository.GetByIdAsync(linkedPropertyProfile.PropertyProfileId, cancellationToken);

        var propertyProfileCompartments = propertyProfile.Value.Compartments;

        // Ensure no GISData is missing
        var relevantPropertyProfileCompartments = propertyProfileCompartments.Where(p => selectedCompartmentIds.Contains(p.Id));
        if (relevantPropertyProfileCompartments.Count() == 0 || relevantPropertyProfileCompartments.Any(x=>x.GISData == null))
        {
            errorMessage = $"Unable to retrieve selected property compartments for application with identifier {message.ApplicationId} as some GISData is missing.";
            await PublishCentrePointFailures(message, errorMessage, cancellationToken);

            return Result.Failure(errorMessage);
        }

        var centreResult = await _submitFellingLicenceService.CalculateCentrePointForApplicationAsync(message.ApplicationId,
            relevantPropertyProfileCompartments!.Select(c => c.GISData).ToList()!, cancellationToken);
        if (centreResult.IsFailure)
        {
            errorMessage = $"Unable to calculate centre point for application having id {message.ApplicationId}";
            await PublishCentrePointFailures(message, errorMessage, cancellationToken);

            return Result.Failure(errorMessage);
        }

        var osGrid = await _submitFellingLicenceService.CalculateOSGridAsync(centreResult.Value, cancellationToken);
        if (osGrid.IsFailure)
        {
            errorMessage = $"Unable to calculate OS Grid Ref for application having id {message.ApplicationId}";
            await PublishCentrePointFailures(message, errorMessage, cancellationToken);
            
            return Result.Failure(errorMessage);
        }

        var getConfiguredFcArea = await _submitFellingLicenceService.GetConfiguredFcAreaAsync(centreResult.Value, cancellationToken);
        if (getConfiguredFcArea.IsFailure)
        {
            errorMessage = $"Unable to retrieve the configured FC Area for application having id {message.ApplicationId}";
            await PublishCentrePointFailures(message, errorMessage, cancellationToken);

            return Result.Failure(errorMessage);
        }

        var updateFla =
            await _updateCentrePoint.UpdateCentrePointAsync(
                message.ApplicationId, 
                userAccModel, 
                getConfiguredFcArea.Value.AreaCostCode, 
                getConfiguredFcArea.Value.AdminHubName,
                centreResult.Value, 
                osGrid.Value, 
                cancellationToken);

        if (updateFla.IsFailure)
        {
            _logger.LogError("Unable to update the Area Code, Admin Region, Centre Point, and OS grid reference for application of id {ApplicationId}, with error {Error}",
                message.ApplicationId, updateFla.Error);
            errorMessage = $"Unable to update the Area Code, Admin Region, Centre Point, and OS grid reference for application of id {message.ApplicationId}, with error {updateFla.Error}";
            await PublishCentrePointFailures(message, errorMessage, cancellationToken);

            return Result.Failure(errorMessage);
        }

        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.CalculateCentrePointForApplication,
                message.ApplicationId,
                message.UserId,
                _requestContext,
                new
                {
                    WoodlandOwner = message.WoodlandOwnerId,
                    CentrePoint = centreResult.Value,
                    OsGridReference = osGrid.Value,
                    ConfiguredFcArea = getConfiguredFcArea.Value
                }),
            cancellationToken);

        return Result.Success();
    }

    private async Task PublishCentrePointFailures(
        CentrePointCalculationMessage message,
        string errorMessage,
        CancellationToken cancellationToken,
        Exception? ex = null)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
                AuditEvents.CalculateCentrePointForApplicationFailure,
                message.ApplicationId,
                message.UserId,
                _requestContext,
                new
                {
                    WoodlandOwner = message.WoodlandOwnerId,
                    Error = errorMessage
                }),
            cancellationToken);

        if (ex is not null)
        {
            _logger.LogError(ex,
                "Error when updating centre point, application id: {FellingLicenceApplicationId}",
                message.ApplicationId);
        }
    }
}