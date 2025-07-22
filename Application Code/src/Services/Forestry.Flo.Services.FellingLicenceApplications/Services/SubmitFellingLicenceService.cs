using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Repositories;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NodaTime;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class SubmitFellingLicenceService : ISubmitFellingLicenceService
{
    private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationInternalRepository;
    private readonly ISendNotifications _notificationsService;
    private readonly IGetConfiguredFcAreas _getConfiguredFcAreasService;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IForesterServices _iForesterServices;
    private readonly IForestryServices _forestryServices;
    private readonly IClock _clock;
    private readonly ILogger<SubmitFellingLicenceService> _logger;
    private readonly IAuditService<SubmitFellingLicenceService> _audit;
    private readonly RequestContext _requestContext;

    public SubmitFellingLicenceService(
        IAuditService<SubmitFellingLicenceService> auditService,
        RequestContext requestContext,
        ILogger<SubmitFellingLicenceService> logger,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IUserAccountRepository userAccountRepository,
        IForesterServices iForesterServices,
        IForestryServices forestryServices,
        IClock clock,
        ISendNotifications notificationsService,
        IGetConfiguredFcAreas getConfiguredFcAreasService)
    {
        _fellingLicenceApplicationInternalRepository = Guard.Against.Null(fellingLicenceApplicationInternalRepository);
        _userAccountRepository = Guard.Against.Null(userAccountRepository);
        _iForesterServices = Guard.Against.Null(iForesterServices);
        _forestryServices = Guard.Against.Null(forestryServices);
        _clock = Guard.Against.Null(clock);
        _logger = Guard.Against.Null(logger);
        _audit = Guard.Against.Null(auditService);
        _requestContext = Guard.Against.Null(requestContext);
        _notificationsService = Guard.Against.Null(notificationsService);
        _getConfiguredFcAreasService = Guard.Against.Null(getConfiguredFcAreasService);
    }

    public async Task<Result<AutoAssignWoRecord>> AutoAssignWoodlandOfficerAsync(
        Guid applicationId,
        Guid externalApplicantId,
        string linkToApplication,
        CancellationToken cancellationToken)
    {
        var applicationMaybe = await _fellingLicenceApplicationInternalRepository.GetAsync(applicationId, cancellationToken);

        if (applicationMaybe.HasNoValue)
        {
            _logger.LogError("Unable to retrieve felling licence application of id {id}", applicationId);

            await AutoAssignWoodlandOfficerFailureEventAsync(
                applicationId,
                externalApplicantId,
                null,
                "Unable to retrieve felling licence application",
                cancellationToken);

            return Result.Failure<AutoAssignWoRecord>($"Unable to retrieve felling licence application of id {applicationId}");
        }

        var application = applicationMaybe.Value;

        Point centrePoint = null;

        if (!string.IsNullOrEmpty(application.CentrePoint))
        {
            try
            {
                centrePoint =
                    JsonConvert.DeserializeObject<Point>(application.CentrePoint);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unable to retrieve woodland officer response for auto-assigning for application of id {id},incorrect centre point {CentrePoint}", applicationId, application.CentrePoint!);

                await AutoAssignWoodlandOfficerFailureEventAsync(
                    applicationId,
                    externalApplicantId,
                    null,
                    "Unable to retrieve woodland officer response for auto-assigning, bad centre point",
                    cancellationToken);

                return Result.Failure<AutoAssignWoRecord>($"Unable to retrieve woodland officer response for auto-assigning for application of id {applicationId} bab center point {application.CentrePoint!}");
            }
        }

        if(centrePoint ==null)
        {
            _logger.LogError("Unable to retrieve woodland officer response for auto-assigning for application of id {id}, no centre point set", applicationId);

            await AutoAssignWoodlandOfficerFailureEventAsync(
                applicationId,
                externalApplicantId,
                null,
                "Unable to retrieve woodland officer response for auto-assigning, bad centre point",
                cancellationToken);

            return Result.Failure<AutoAssignWoRecord>($"Unable to retrieve woodland officer response for auto-assigning for application of id {applicationId}");
        }
        var (_, isFailureWoodlandOfficer, woodlandOfficerResponse) = await _iForesterServices.GetWoodlandOfficerAsync(centrePoint, cancellationToken);

        if (isFailureWoodlandOfficer)
        {
            _logger.LogError("Unable to retrieve woodland officer response for auto-assigning for application of id {id}", applicationId);

            await AutoAssignWoodlandOfficerFailureEventAsync(
                applicationId,
                externalApplicantId,
                null,
                "Unable to retrieve woodland officer response for auto-assigning",
                cancellationToken);

            return Result.Failure<AutoAssignWoRecord>($"Unable to retrieve woodland officer response for auto-assigning for application of id {applicationId}");
        }

        var internalUserMaybe = await DetermineInternalUserFromFullName(
            woodlandOfficerResponse.OfficerName!,
            cancellationToken);

        if (internalUserMaybe.HasNoValue)
        {
            _logger.LogError("Unable to match woodland officer response to an internal user account for application of id {id}", applicationId);

            await AutoAssignWoodlandOfficerFailureEventAsync(
                applicationId,
                externalApplicantId,
                null,
                "Unable to match woodland officer response to an internal user account",
                cancellationToken);

            return Result.Failure<AutoAssignWoRecord>($"Unable to match woodland officer response to an internal user account for application of id {applicationId}");
        }

        var internalUser = internalUserMaybe.Value;

        // Return if the matched internal user is already the assigned woodland officer

        if (application.AssigneeHistories.Any(x =>
                x.AssignedUserId == internalUser.Id && x.TimestampUnassigned is null))
        {
            _logger.LogInformation("Woodland officer is already assigned to the application, application id {appId}, woodland officer id: {woId}", applicationId, internalUser.Id);
            return Result.Failure<AutoAssignWoRecord>("Woodland officer already assigned to the application");
        }

        var assignResult = await _fellingLicenceApplicationInternalRepository.AssignFellingLicenceApplicationToStaffMemberAsync(
            applicationId,
            internalUser.Id,
            AssignedUserRole.WoodlandOfficer,
            _clock.GetCurrentInstant().ToDateTimeUtc(),
            cancellationToken);

        return Result.Success(new AutoAssignWoRecord(
            internalUser.Id,
            internalUser.FirstName!,
            internalUser.LastName!,
            internalUser.Email,
            assignResult.UserUnassigned.HasValue ? assignResult.UserUnassigned.Value : null));
    }

    private async Task<Maybe<UserAccount>> DetermineInternalUserFromFullName(string fullname, CancellationToken cancellationToken)
    {

        // Attempt all permutations of the full name string split into first and last names.

        var names = fullname.Trim().Split(' ');

        var locatedAccount = Maybe<UserAccount>.None;

        for (var i = 0; i < names.Length - 1; i++)
        {
            var firstName = string.Join(" ", names.SkipLast(names.Length - i - 1));
            var surname = string.Join(" ", names.Skip(i + 1));

            var locatedAccounts = await _userAccountRepository.GetByFullnameAsync(firstName, surname, cancellationToken);

            if (!locatedAccounts.Any())
            {
                continue;
            };

            if (locatedAccounts.Count > 1)
            {
                _logger.LogWarning("Multiple internal users were found with the name: '{firstName} {surname}'. The first has been selected", firstName, surname);
            }

            locatedAccount = Maybe<UserAccount>.From(locatedAccounts.First());

            break;
        }

        return locatedAccount;
    }


    /// <inheritdoc />
    public async Task<Result<string>> CalculateCentrePointForApplicationAsync(Guid applicationId,
        List<string> compartments,
        CancellationToken cancellationToken)
    {

        var centreResult = await _forestryServices.CalculateCentrePointAsync(compartments,
            cancellationToken);

        if (!centreResult.IsFailure)
        {
            return Result.Success(centreResult.Value.GetGeometrySimple());
        }
        _logger.LogError("Unable to calculate centre point for application of id {id}", applicationId);
        return Result.Failure<string>($"Unable to calculate centre point for application of id {applicationId}");
    }

    /// <inheritdoc />
    public async Task<Result<string>> CalculateOSGridAsync(string centrePointString,
        CancellationToken cancellationToken)
    {
        Point? centre = null;

        try
        {
            if(!string.IsNullOrEmpty(centrePointString))
            {
                centre = JsonConvert.DeserializeObject<Point>(centrePointString);
            }
        }

        catch (Exception)
        {
            _logger.LogError("Unable to calculate OS point \"{id}\"", centrePointString);
        }

        return centre != null
            ? await _forestryServices.GetOSGridReferenceAsync(centre, cancellationToken)
            : Result.Failure<string>($"Unable to convert Point \"{centrePointString}\"");
    }

    /// <inheritdoc />
    public async Task<Result<ConfiguredFcArea>> GetConfiguredFcAreaAsync(string centrePointString, CancellationToken cancellationToken)
    {
        Point? centre = null;

        try
        {
            if (!string.IsNullOrEmpty(centrePointString))
            {
                centre = JsonConvert.DeserializeObject<Point>(centrePointString);
            }
        }
        catch (Exception)
        {
            _logger.LogError("Unable to calculate OS point \"{id}\"", centrePointString);
        }

        if (centre != null)
        {
            var getAdminBoundary = await _iForesterServices.GetAdminBoundaryIdAsync(centre, cancellationToken);
            if (getAdminBoundary.IsFailure)
            {
                _logger.LogError("Unable to obtain the configured FC area for {centrePointString}", centrePointString);
                return Result.Failure<ConfiguredFcArea>($"Unable to obtain the area code for \"{centrePointString}\"");
            }

            var areaCode = getAdminBoundary.Value.Code;

            _logger.LogDebug("Getting the configured FC area for the admin area code of {AreaCode}", areaCode);

            var configuredFcAreasResult = await _getConfiguredFcAreasService.GetAllAsync(cancellationToken);

            if (configuredFcAreasResult.IsFailure)
            {
                _logger.LogError("Unable to retrieve the configured FC areas");

                return Result.Failure<ConfiguredFcArea>(
                    $"Unable to return the configured FC area for the centre point");
            }

            var configuredFcArea = configuredFcAreasResult.Value
                .Single(x => x.Area.Code == areaCode);

            return Result.Success(configuredFcArea);
        }

        return Result.Failure<ConfiguredFcArea>($"Unable to obtain the configured FC area for \"{centrePointString}\"");
    }

    private async Task AutoAssignWoodlandOfficerFailureEventAsync(
        Guid applicationId,
        Guid externalApplicantId,
        Guid? assignedStaffMemberId,
        string error,
        CancellationToken cancellationToken)
    {
        await _audit.PublishAuditEventAsync(
            new AuditEvent(
                AuditEvents.AssignFellingLicenceApplicationToStaffMemberFailure,
                applicationId,
                externalApplicantId,
                _requestContext,
                new
                {
                    AssignedUserRole = AssignedUserRole.WoodlandOfficer.GetDisplayName()!,
                    AssignedStaffMemberId = assignedStaffMemberId,
                    Error = error,
                }),
            cancellationToken);
    }
}