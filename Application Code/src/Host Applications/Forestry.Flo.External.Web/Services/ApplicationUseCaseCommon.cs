using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Extensions;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.PropertyProfiles;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Services;

namespace Forestry.Flo.External.Web.Services;

public class ApplicationUseCaseCommon
{
    private readonly IRetrieveWoodlandOwners _retrieveWoodlandOwnersService;
    private readonly IRetrieveUserAccountsService _retrieveUserAccountsService;
    private readonly IGetPropertyProfiles _getPropertyProfilesService;
    private readonly IGetCompartments _getCompartmentsService;
    private readonly IAgentAuthorityService _agentAuthorityService;
    private readonly ILogger<ApplicationUseCaseCommon> _logger;

    protected readonly IGetFellingLicenceApplicationForExternalUsers GetFellingLicenceApplicationServiceForExternalUsers;

    public ApplicationUseCaseCommon(
        IRetrieveUserAccountsService retrieveUserAccountsService,
        IRetrieveWoodlandOwners retrieveWoodlandOwnersService,
        IGetFellingLicenceApplicationForExternalUsers getFellingLicenceApplicationServiceForExternalUsers, 
        IGetPropertyProfiles getPropertyProfilesService,
        IGetCompartments getCompartmentsService, 
        IAgentAuthorityService agentAuthorityService,
        ILogger<ApplicationUseCaseCommon> logger)
    {
        _retrieveWoodlandOwnersService = Guard.Against.Null(retrieveWoodlandOwnersService);
        _getCompartmentsService = Guard.Against.Null(getCompartmentsService);
        _getPropertyProfilesService = Guard.Against.Null(getPropertyProfilesService);
        _retrieveUserAccountsService = Guard.Against.Null(retrieveUserAccountsService);
        _agentAuthorityService = Guard.Against.Null(agentAuthorityService);
        GetFellingLicenceApplicationServiceForExternalUsers = Guard.Against.Null(getFellingLicenceApplicationServiceForExternalUsers);
        _logger = logger;
    }

    public virtual async Task<Result> EnsureApplicationIsEditable(
        Guid fellingLicenceApplicationId, 
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var userAccess = await GetUserAccessModelAsync(user, cancellationToken);

        if (userAccess.IsFailure)
        {
            return Result.Failure($"Attempt to update a Felling Licence Application with a non editable status is not allowed. ID: {fellingLicenceApplicationId}");
        }

        var isEditable = await GetFellingLicenceApplicationServiceForExternalUsers.GetIsEditable(
            fellingLicenceApplicationId, 
            userAccess.Value,
            cancellationToken);

        if (isEditable.IsFailure || !isEditable.Value)
        {
            // Backend protection against updating submitted FLAs in case UI protections fail.
            return Result.Failure($"Attempt to update a Felling Licence Application with a non editable status is not allowed. ID: {fellingLicenceApplicationId}");
        }

        return Result.Success();
    }

    /// <summary>
    /// Gets a felling licence application by its unique Id
    /// performing user access checks before retrieval.
    /// </summary>
    /// <param name="applicationId">The unique id of the application to get</param>
    /// <param name="user">The external applicant user to be checked against access control before returning the application</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A result object containing the <see cref="FellingLicenceApplication"/> or a failure object with the failure reason</returns>
    protected async Task<Result<FellingLicenceApplication>> GetFellingLicenceApplicationAsync(
        Guid applicationId, 
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var userAccess = await GetUserAccessModelAsync(user, cancellationToken);

        if (userAccess.IsFailure)
        {
            return Result.Failure<FellingLicenceApplication> ($"Attempt to access Felling Licence Application with id: {applicationId} by user with Id of {user.UserAccountId} resulted in access being denied");
        }

        return await GetFellingLicenceApplicationServiceForExternalUsers.GetApplicationByIdAsync(
            applicationId,
            userAccess.Value,
            cancellationToken);
    }

    /// <summary>
    /// Gets a property profile by its unique Id
    /// performing user access checks before retrieval.
    /// </summary>
    /// <param name="propertyProfileId">The unique id of the property to get</param>
    /// <param name="user">The external applicant user to be checked against access control before returning the property profile</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A result object containing the <see cref="PropertyProfile"/> or a failure object with the failure reason</returns>
    protected async Task<Result<PropertyProfile>> GetPropertyProfileByIdAsync(
        Guid propertyProfileId,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var userAccess = await GetUserAccessModelAsync(user, cancellationToken);

        if (userAccess.IsFailure)
        {
            return Result.Failure<PropertyProfile>($"Attempt to access property profile with id: {propertyProfileId} by user with Id of {user.UserAccountId} resulted in access being denied");
        }

        return await _getPropertyProfilesService.GetPropertyByIdAsync(
            propertyProfileId, 
            userAccess.Value, 
            cancellationToken);
    }

    /// <summary>
    /// Gets a property profile by its unique Id
    /// performing user access checks before retrieval.
    /// </summary>
    /// <param name="query">The <see cref="ListPropertyProfilesQuery"/> query to execute against property profiles</param>
    /// <param name="user">The external applicant user to be checked against access control before returning the property profile</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A result object containing the <see cref="PropertyProfile"/> or a failure object with the failure reason</returns>
    protected async Task<Result<IEnumerable<PropertyProfile>>> GetPropertyProfilesByIdAsync(
        ListPropertyProfilesQuery query,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var userAccessModel = await GetUserAccessModelAsync(user, cancellationToken);

        if (userAccessModel.IsFailure)
        {
            return Result.Failure<IEnumerable<PropertyProfile>>($"Attempt to access property profiles with id: {query.Ids} by user with Id of {user.UserAccountId} resulted in access being denied");
        }

        return await _getPropertyProfilesService.ListAsync(
            query,
            userAccessModel.Value,
            cancellationToken);
    }

    /// <summary>
    /// Gets a compartment by its unique Id performing user access checks before retrieval.
    /// </summary>
    /// <param name="compartmentId">The id of the compartment to get</param>
    /// <param name="user">The external applicant user to be checked against access control before returning the compartment</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A result object containing the <see cref="PropertyProfile"/> or a failure object with the failure reason</returns>
    protected async Task<Result<Compartment>> GetCompartmentByIdAsync(
        Guid compartmentId,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var userAccess = await GetUserAccessModelAsync(user, cancellationToken);

        if (userAccess.IsFailure)
        {
            return Result.Failure<Compartment>($"Attempt to access compartment with id: {compartmentId} by user with Id of {user.UserAccountId} resulted in access being denied");
        }

        return await _getCompartmentsService.GetCompartmentByIdAsync(
            compartmentId,
            userAccess.Value,
            cancellationToken);
    }
    
    protected async Task<Result<UserAccessModel>> GetUserAccessModelAsync(
        ExternalApplicant user, 
        CancellationToken cancellationToken)
    {
        return await _retrieveUserAccountsService
            .RetrieveUserAccessAsync(user.UserAccountId!.Value, cancellationToken)
            .ConfigureAwait(false);
    }

    protected async Task<Result<WoodlandOwnerWithAgencyRecord>> GetWoodlandOwnerNameAndAgencyForApplication(
        FellingLicenceApplication application, 
        CancellationToken cancellationToken)
    {
        var getWoodlandOwnerDetailsResult = await GetApplicationWoodlandOwnerNameByIdAsync(application.WoodlandOwnerId, cancellationToken);
        if (getWoodlandOwnerDetailsResult.IsFailure)
        {
            _logger.LogError("Could not retrieve woodland owner name detail for {WoodlandOwnerId}, error was {Error}",
                application.WoodlandOwnerId, getWoodlandOwnerDetailsResult.Error);
            return Result.Failure<WoodlandOwnerWithAgencyRecord>(getWoodlandOwnerDetailsResult.Error);
        }

        var getAgencyDetailsResult = await GetApplicationAgencyNameAsync(application.WoodlandOwnerId, cancellationToken);

        var woodlandOwnerName = getWoodlandOwnerDetailsResult.Value;
        var agencyName = getAgencyDetailsResult.HasValue ? getAgencyDetailsResult.Value : null;

        return new WoodlandOwnerWithAgencyRecord(agencyName, woodlandOwnerName);
    }

    protected Task<Maybe<AgencyModel>> GetAgencyModelForWoodlandOwnerAsync(
        Guid woodlandOwnerId,
        CancellationToken cancellationToken)
    {
        return _agentAuthorityService.GetAgencyForWoodlandOwnerAsync(woodlandOwnerId, cancellationToken);
    }

    public async Task<Maybe<AgentAuthorityModel>> GetWoodlandOwnerAafAsync(
        Guid woodlandOwnerId,
        CancellationToken cancellationToken)
    {
        return await _agentAuthorityService.GetAgentAuthorityForWoodlandOwnerAsync(woodlandOwnerId, cancellationToken);
    }

    protected Task<Result<WoodlandOwnerModel>> GetWoodlandOwnerByIdAsync(
        Guid woodlandOwnerId,
        CancellationToken cancellationToken)
    {
        return _retrieveWoodlandOwnersService.RetrieveWoodlandOwnerByIdAsync(woodlandOwnerId, cancellationToken);
    }

    protected async Task<Result<FellingLicenceApplicationSummary>> GetApplicationSummaryAsync(
        FellingLicenceApplication application,
        ExternalApplicant user,
        CancellationToken cancellationToken)
    {
        var userAccess = await GetUserAccessModelAsync(user, cancellationToken);

        if (userAccess.IsFailure)
        {
            return Result.Failure<FellingLicenceApplicationSummary>($"Attempt to retrieve summary for application with id: {application.Id} by user with Id of {user.UserAccountId} resulted in access being denied");
        }

        string? propertyName = null;
        string? propertyNameOfWood = null;

        if (application.IsInApplicantEditableState() == false)
        {
            var submittedProperty = await GetFellingLicenceApplicationServiceForExternalUsers
                .GetExistingSubmittedFlaPropertyDetailAsync(application.Id, userAccess.Value, cancellationToken);

            if (submittedProperty.IsFailure)
            {
                _logger.LogWarning("Unable to get submitted property for application, error : {Error}", submittedProperty.Error);
                return submittedProperty.ConvertFailure<FellingLicenceApplicationSummary>();
            }

            if (submittedProperty.Value.HasValue)
            {
                propertyName = submittedProperty.Value.Value.Name;
                propertyNameOfWood = submittedProperty.Value.Value.NameOfWood;
            }
        }
        else
        {
            var profile = await GetPropertyProfileByIdAsync(
                application.LinkedPropertyProfile!.PropertyProfileId,
                user,
                cancellationToken);

            if (profile.IsFailure)
            {
                _logger.LogWarning("Unable to get property for application, error : {Error}", profile.Error);
                return profile.ConvertFailure<FellingLicenceApplicationSummary>();
            }

            propertyName = profile.Value.Name;
            propertyNameOfWood = profile.Value.NameOfWood;
        }

        var woodlandOwnerNameAndAgencyDetails =
            await GetWoodlandOwnerNameAndAgencyForApplication(application, cancellationToken);

        if (woodlandOwnerNameAndAgencyDetails.IsFailure)
        {
            _logger.LogWarning("Unable to get woodland owner name and agency for application, error : {Error}", woodlandOwnerNameAndAgencyDetails.Error);
            return woodlandOwnerNameAndAgencyDetails.ConvertFailure<FellingLicenceApplicationSummary>();
        }

        return new FellingLicenceApplicationSummary(
            application.Id,
            application.ApplicationReference,
            application.GetCurrentStatus(),
            propertyName,
            application.LinkedPropertyProfile?.PropertyProfileId ?? Guid.Empty,
            propertyNameOfWood,
            application.WoodlandOwnerId,
            woodlandOwnerNameAndAgencyDetails.Value.WoodlandOwnerName,
            woodlandOwnerNameAndAgencyDetails.Value.AgencyName);

    }

    private async Task<Result<string>> GetApplicationWoodlandOwnerNameByIdAsync(
        Guid woodlandOwnerId,
        CancellationToken cancellationToken)
    {
        var woodlandOwnerResult = await _retrieveWoodlandOwnersService.RetrieveWoodlandOwnerByIdAsync(woodlandOwnerId, cancellationToken);

        if (woodlandOwnerResult.IsFailure)
        {
            _logger.LogError("Could not retrieve woodland owner name detail for {WoodlandOwnerId}, error was {Error}",
                woodlandOwnerId, woodlandOwnerResult.Error);
           
            return Result.Failure<string>(woodlandOwnerResult.Error);
        }

        return Result.Success(woodlandOwnerResult.Value.GetContactNameForDisplay);
    }

    private async Task<Maybe<string>> GetApplicationAgencyNameAsync(
        Guid woodlandOwnerId,
        CancellationToken cancellationToken)
    {
        var agency = await _agentAuthorityService
            .GetAgencyForWoodlandOwnerAsync(woodlandOwnerId, cancellationToken)
            .ConfigureAwait(false);

        if (agency.HasValue)
        {
            var agentOrAgencyName = agency.Value.OrganisationName ?? agency.Value.ContactName;
            return agentOrAgencyName.AsMaybe();
        }

        return Maybe<string>.None;
    }
}

public record WoodlandOwnerWithAgencyRecord (string? AgencyName = null, string? WoodlandOwnerName = null);
