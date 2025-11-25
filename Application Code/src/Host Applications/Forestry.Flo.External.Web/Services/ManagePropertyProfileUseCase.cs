using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.PropertyProfile;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Forestry.Flo.Services.PropertyProfiles.Services;

namespace Forestry.Flo.External.Web.Services;

public class ManagePropertyProfileUseCase
{
    private readonly IPropertyProfileRepository _propertyProfileRepository;
    private readonly IGetPropertyProfiles _getPropertyProfilesService;
    private readonly IRetrieveUserAccountsService _retrieveUserAccountsService;
    private readonly IAuditService<ManagePropertyProfileUseCase> _auditService;
    private readonly RequestContext _requestContext;
    private readonly ILogger<ManagePropertyProfileUseCase> _logger;

    public ManagePropertyProfileUseCase(
        IPropertyProfileRepository propertyProfileRepository,
        IGetPropertyProfiles getPropertyProfilesService,
        IAuditService<ManagePropertyProfileUseCase> auditService,
        IRetrieveUserAccountsService retrieveUserAccountsService,
        RequestContext requestContext,
        ILogger<ManagePropertyProfileUseCase> logger)
    {
        _propertyProfileRepository = propertyProfileRepository;
        _retrieveUserAccountsService = Guard.Against.Null(retrieveUserAccountsService);
        _getPropertyProfilesService = Guard.Against.Null(getPropertyProfilesService);
        _auditService = auditService;
        _requestContext = Guard.Against.Null(requestContext);
        _logger = logger;
    }

    /// <summary>
    /// Creates property profile and saves it in database
    /// </summary>
    /// <param name="propertyProfile">A model the representing property profile <see cref="PropertyProfileModel"/> to create</param>
    /// <param name="user">The current user.</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>Success with a created property profile model or failure of the operation.</returns>
    public async Task<Result<PropertyProfileModel, ErrorDetails>> CreatePropertyProfile(
        PropertyProfileModel propertyProfile,
        ExternalApplicant user,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(propertyProfile);
        Guard.Against.Null(user);

        var propertyProfileEntity = ModelMapping.ToPropertyProfile(propertyProfile);

        var profile = _propertyProfileRepository.Add(propertyProfileEntity);
        return await _propertyProfileRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
            .Tap(async () =>
                await _auditService.PublishAuditEventAsync(
                    new AuditEvent(
                        AuditEvents.CreatePropertyProfileEvent,
                        profile.Id,
                        user.UserAccountId,
                        _requestContext,
                        new { propertyProfile.Name }),
                    cancellationToken))
            .OnFailure(async r =>
                await HandlePropertyProfileError(propertyProfile, user,
                    AuditEvents.CreatePropertyProfileFailureEvent, r, cancellationToken))
            .Map(() => ModelMapping.ToPropertyProfileModel(profile))
            .MapError(e => e == UserDbErrorReason.NotUnique ? 
                new ErrorDetails(ErrorTypes.Conflict, nameof(profile.Name)) :
                new ErrorDetails(ErrorTypes.InternalError));
    }

    /// <summary>
    /// Finds and returns a property profile by a property profile id
    /// </summary>
    /// <param name="propertyProfileId">A property profile id to search for a property profile</param>
    /// <param name="user">An application user</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>A found property profile or an empty response</returns>
    public async Task<Maybe<PropertyProfileModel>> RetrievePropertyProfileAsync(
        Guid propertyProfileId,
        ExternalApplicant user,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(user);

        var userAccessModel = await _retrieveUserAccountsService
            .RetrieveUserAccessAsync(user.UserAccountId!.Value, cancellationToken)
            .ConfigureAwait(false);
        
        var propertyProfile =
            await _getPropertyProfilesService
                .GetPropertyByIdAsync(propertyProfileId, userAccessModel.Value, cancellationToken)
                .OnFailure(_ =>
                    _logger.LogError(
                        "Property profile with id: {PropertyProfileId} for user having id: {UserId} could not be retrieved",
                        propertyProfileId, user.UserAccountId));

        return propertyProfile.IsFailure
            ? Maybe<PropertyProfileModel>.None
            : Maybe<PropertyProfileModel>.From(ModelMapping.ToPropertyProfileModel(propertyProfile.Value));
    }

    public async Task<Maybe<IEnumerable<PropertyProfileModel>>> RetrievePropertyProfilesAsync(
        Guid woodlandOwnerId,
        ExternalApplicant user,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(woodlandOwnerId);

        var userAccessModel = await _retrieveUserAccountsService
            .RetrieveUserAccessAsync(user.UserAccountId!.Value, cancellationToken)
            .ConfigureAwait(false);

        if (userAccessModel.IsFailure)
        {
            _logger.LogError("Could not retrieve user access");
            return Maybe<IEnumerable<PropertyProfileModel>>.None;
        }

        var propertyProfiles =
            await _getPropertyProfilesService
            .ListByWoodlandOwnerAsync(woodlandOwnerId, userAccessModel.Value, cancellationToken)
            .OnFailure(_ =>
                    _logger.LogError(
                        "Property profiles for woodland owner: {woodlandOwnerId} could not be retrieved",
                        woodlandOwnerId));

        return propertyProfiles.IsFailure
            ? Maybe<IEnumerable<PropertyProfileModel>>.None
            : Maybe<IEnumerable<PropertyProfileModel>>.From(ModelMapping.ToPropertyProfileModel(propertyProfiles.Value));
    }
    
    /// <summary>
    /// Returns an object containing compartment list and required
    /// property profile attributes
    /// </summary>
    /// <param name="propertyProfileId">A property profile id</param>
    /// <param name="user">An application user</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>Object containing compartment list and required property profile attributes</returns>
    public async Task<Maybe<PropertyProfileDetails>> RetrievePropertyProfileCompartments(
        Guid propertyProfileId,
        ExternalApplicant user,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(user);

        var userAccessModel = await _retrieveUserAccountsService
            .RetrieveUserAccessAsync(user.UserAccountId!.Value, cancellationToken)
            .ConfigureAwait(false);

        var (_, isFailure, propertyProfile) = await _getPropertyProfilesService.GetPropertyByIdAsync(propertyProfileId, userAccessModel.Value, cancellationToken)
            .OnFailure(_ => _logger.LogError(
                "Property profile with id: {PropertyProfileId} for user having id: {UserId} could not be retrieved",
                propertyProfileId, user.UserAccountId));
            
        if (isFailure)
        {
            return Maybe<PropertyProfileDetails>.None;
        }

        var compartmentModels = ModelMapping.ToCompartmentModelList(propertyProfile.Compartments);

        return Maybe<PropertyProfileDetails>.From(new PropertyProfileDetails
        {
            Id = propertyProfile.Id,
            Name = propertyProfile.Name,
            NearestTown = propertyProfile.NearestTown,
            NameOfWood = propertyProfile.NameOfWood,
            WoodlandOwnerId = propertyProfile.WoodlandOwnerId,
            WoodlandCertificationSchemeReference = propertyProfile.WoodlandCertificationSchemeReference,
            WoodlandManagementPlanReference = propertyProfile.WoodlandManagementPlanReference,
            Compartments = compartmentModels.OrderByNameNumericOrAlpha()
        });


    }
    
    /// <summary>
    /// Updates property profile and saves it in database
    /// </summary>
    /// <param name="propertyProfile">A model the representing property profile <see cref="PropertyProfileModel"/> to update</param>
    /// <param name="user"></param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>Success or failure of the operation</returns>
    public async Task<Result<PropertyProfileModel, ErrorDetails>> EditPropertyProfile(
        PropertyProfileModel propertyProfile,
        ExternalApplicant user,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(propertyProfile);
        Guard.Against.Null(user);

        var userAccessModel = await _retrieveUserAccountsService
            .RetrieveUserAccessAsync(user.UserAccountId!.Value, cancellationToken)
            .ConfigureAwait(false);


        if (!userAccessModel.Value.IsFcUser && !userAccessModel.Value.WoodlandOwnerIds!.Contains(propertyProfile.WoodlandOwnerId))
        {
            _logger.LogError(
                "User with id: {userId} does not have access to the property with id: {PropertyProfileId}",
                user.UserAccountId, propertyProfile.Id);
            return Result.Failure<PropertyProfileModel, ErrorDetails>(new ErrorDetails(ErrorTypes.NotAuthorised));
        }

        var profile = ModelMapping.ToPropertyProfile(propertyProfile);

        var getPropertyResult = await _getPropertyProfilesService.GetPropertyByIdAsync(profile.Id, userAccessModel.Value, cancellationToken);

        if (!getPropertyResult.IsSuccess)
        {
            await HandlePropertyProfileError(propertyProfile, user,
                AuditEvents.UpdatePropertyProfileFailureEvent, UserDbErrorReason.NotFound,
                cancellationToken);

            return new ErrorDetails(ErrorTypes.NotFound);
        }

        return await _propertyProfileRepository.UpdateAsync(profile)
            .Check(async _ => await _propertyProfileRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken))
            .Tap(async () => await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.UpdatePropertyProfileEvent,
                    profile.Id,
                    user.UserAccountId,
                    _requestContext,
                    new { propertyProfile.Name }),
                cancellationToken))
            .OnFailure(async r => await HandlePropertyProfileError(propertyProfile, user,
                AuditEvents.UpdatePropertyProfileFailureEvent, r,
                cancellationToken))
            .Map(ModelMapping.ToPropertyProfileModel)
            .MapError(e => e switch
            {
                UserDbErrorReason.NotUnique => new ErrorDetails(ErrorTypes.Conflict, nameof(profile.Name)),
                UserDbErrorReason.NotFound => new ErrorDetails(ErrorTypes.NotFound),
                _ => new ErrorDetails(ErrorTypes.InternalError)
            });
    }

    private async Task HandlePropertyProfileError(IPropertyWithBreadcrumbsViewModel propertyProfile,
        ExternalApplicant user, string eventName, UserDbErrorReason errorReason, CancellationToken cancellationToken)
    {
        var error = errorReason == UserDbErrorReason.NotUnique
            ? $"A property profile with the name: {propertyProfile.Name} already exists"
            : $"An error occurred  during {eventName}";
        var auditEvent = new AuditEvent(eventName, null, user.UserAccountId, _requestContext,
            new { propertyProfile.Name, Error = error });
        await _auditService.PublishAuditEventAsync(auditEvent, cancellationToken);
    }
}