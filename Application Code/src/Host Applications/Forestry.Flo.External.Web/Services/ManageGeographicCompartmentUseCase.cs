using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Models.Compartment;
using Forestry.Flo.External.Web.Models.PropertyProfile;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.Gis.Models.Internal.Request;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Forestry.Flo.Services.PropertyProfiles.Services;

namespace Forestry.Flo.External.Web.Services;

public class ManageGeographicCompartmentUseCase
{
    private readonly IGetPropertyProfiles _getPropertyProfilesService;
    private readonly IRetrieveUserAccountsService _retrieveUserAccountsService;
    private readonly IGetCompartments _getCompartmentsService;
    private readonly ICompartmentRepository _compartmentRepository;
    private readonly IAuditService<ManageGeographicCompartmentUseCase> _auditService;
    private readonly RequestContext _requestContext;
    private readonly ILogger<ManageGeographicCompartmentUseCase> _logger;

    public ManageGeographicCompartmentUseCase(
        IGetPropertyProfiles getPropertyProfilesService,
        IGetCompartments getCompartmentsService,
        IRetrieveUserAccountsService retrieveUserAccountsService,
        ICompartmentRepository compartmentRepository,
        IAuditService<ManageGeographicCompartmentUseCase> auditService,
        RequestContext requestContext,
        ILogger<ManageGeographicCompartmentUseCase> logger)
    {
        _getPropertyProfilesService = Guard.Against.Null(getPropertyProfilesService);
        _retrieveUserAccountsService = Guard.Against.Null(retrieveUserAccountsService);
        _getCompartmentsService = Guard.Against.Null(getCompartmentsService);
        _compartmentRepository = compartmentRepository;
        _auditService = auditService;
        _requestContext = Guard.Against.Null(requestContext);
        _logger = logger;
    }

    /// <summary>
    /// Creates a Compartment and saves it in database
    /// </summary>
    /// <param name="compartmentModel">A model that representing Compartment <see cref="CompartmentModel"/> to create</param>
    /// <param name="user">The current user.</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>Success or failure of the operation.</returns>
    public async Task<Result<Guid,ErrorDetails>> CreateCompartmentAsync(
        CompartmentModel compartmentModel,
        ExternalApplicant user,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(compartmentModel);
        Guard.Against.Null(user);
        
            var compartmentEntity = ModelMapping.ToCompartment(compartmentModel);
            var compartment = _compartmentRepository.Add(compartmentEntity);

            return await _compartmentRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
                .Tap(async () => await _auditService.PublishAuditEventAsync(
                    new AuditEvent(
                        AuditEvents.CreateCompartmentEvent,
                        compartment.Id,
                        user.UserAccountId,
                        _requestContext,
                        new
                        {
                            compartmentEntity.PropertyProfileId,
                            compartmentEntity.CompartmentNumber,
                            compartmentEntity.SubCompartmentName,
                            compartmentEntity.Designation
                        }),
                    cancellationToken))
                .OnFailure(async r =>
                {
                    await HandleCompartmentError(compartmentModel, user, AuditEvents.CreateCompartmentFailureEvent, r,
                        cancellationToken);
                })
                .Map(() => compartment.Id)
                .MapError(e => e == UserDbErrorReason.NotUnique ? 
                    new ErrorDetails(ErrorTypes.Conflict, nameof(compartment.CompartmentNumber)) :
                    new ErrorDetails(ErrorTypes.InternalError));
      }

    /// <summary>
    /// Creates a Compartment and saves it in database
    /// </summary>
    /// <param name="compartmentImport">A model that representing Compartment <see cref="Compartment"/> to create</param>
    /// <param name="propertyProfileId">The ID of the property</param>
    /// <param name="user">The current user.</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>Success or failure of the operation.</returns>
    public async Task<Result<Guid, ErrorDetails>> CreateCompartmentAsync(
        ImportCompartmentModel compartmentImport, 
        Guid propertyProfileId,
        ExternalApplicant user,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(user);
        Guard.Against.Null(compartmentImport);

        var compartmentModel = new Compartment(
            compartmentImport.CompartmentNumber,
            compartmentImport.SubCompartmentName, 
            compartmentImport.TotalHectares, 
            compartmentImport.Designation, 
            compartmentImport.GISData, 
            propertyProfileId);

        var compartment = _compartmentRepository.Add(compartmentModel);

        return await _compartmentRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken)
            .Tap(async () => await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.CreateCompartmentEvent,
                    compartment.Id,
                    user.UserAccountId,
                    _requestContext,
                    new
                    {
                        compartmentModel.PropertyProfileId,
                        compartmentModel.CompartmentNumber,
                        compartmentModel.SubCompartmentName,
                        compartmentModel.GISData,
                        compartmentModel.TotalHectares,
                        compartmentModel.Designation
                    }),
                cancellationToken))
            .OnFailure(async r =>
            {
                await HandleCompartmentError(compartmentImport, propertyProfileId, user, AuditEvents.CreateCompartmentFailureEvent, r,
                    cancellationToken);
                _compartmentRepository.Remove(compartmentModel);
                _logger.LogError(
                    $"A compartment couldn't be added to the database and rolled back from the stagedSave Details: ProfileID - {compartmentModel.PropertyProfileId}. " +
                    $"Number -{compartmentModel.CompartmentNumber}. " +
                    $"Total HA -{compartmentModel.TotalHectares}. Designation - {compartmentModel.Designation}. " +
                    $"GIS:{compartmentModel.GISData}. Reason: {r.ToString()}");
            })
            .Map(() => compartment.Id)
            .MapError(e => e == UserDbErrorReason.NotUnique ?
                new ErrorDetails(ErrorTypes.Conflict, nameof(compartment.CompartmentNumber)) :
                new ErrorDetails(ErrorTypes.InternalError));
    }

    /// <summary>
    /// Updates a Compartment in database
    /// </summary>
    /// <param name="compartmentModel">A model that representing Compartment <see cref="CompartmentModel"/> to create</param>
    /// <param name="user">An application user</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>Success or failure of the operation.</returns>
    public async Task<UnitResult<ErrorDetails>> EditCompartmentAsync(CompartmentModel compartmentModel,
        ExternalApplicant user,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(compartmentModel);
        Guard.Against.Null(user);
        
        var compartmentEntity = ModelMapping.ToCompartment(compartmentModel);

        var userAccessModel = await GetUserAccessModelAsync(user, cancellationToken);
        var getCompartmentResult = await _getCompartmentsService.GetCompartmentByIdAsync(compartmentEntity.Id, userAccessModel.Value,
            cancellationToken);

        if (!getCompartmentResult.IsSuccess)
        {
            return new ErrorDetails(ErrorTypes.NotFound);
        }

        return await _compartmentRepository.UpdateAsync(compartmentEntity)
            .Check(async _ => await _compartmentRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken))
            .Tap(async r => await _auditService.PublishAuditEventAsync(
                new AuditEvent(
                    AuditEvents.UpdateCompartmentEvent,
                    r.Id,
                    user.UserAccountId,
                    _requestContext,
                    new
                    {
                        compartmentEntity.PropertyProfileId,
                        compartmentEntity.CompartmentNumber,
                        compartmentEntity.SubCompartmentName,
                        compartmentEntity.Designation,
                        compartmentEntity.GISData
                    }),
                cancellationToken))
            .OnFailure(async r =>
            {
                await HandleCompartmentError(compartmentModel, user, AuditEvents.CreateCompartmentFailureEvent, r,
                    cancellationToken);
            })
            .Map(c => c.Id)
            .MapError(e => e switch
            {
                UserDbErrorReason.NotUnique => new ErrorDetails(ErrorTypes.Conflict,
                    nameof(compartmentModel.CompartmentNumber)),
                UserDbErrorReason.NotFound => new ErrorDetails(ErrorTypes.NotFound),
                _ => new ErrorDetails(ErrorTypes.InternalError)
            });
    }

    /// <summary>
    /// Returns a compartment selected by the given id
    /// </summary>
    /// <param name="id">A compartment id</param>
    /// <param name="user">An application user</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns></returns>
    public async Task<Maybe<CompartmentModel>> RetrieveCompartmentAsync(
        Guid id,
        ExternalApplicant user,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(user);

        var userAccessModel = await GetUserAccessModelAsync(user, cancellationToken);

        var compartment =
            await _getCompartmentsService.GetCompartmentByIdAsync(id, userAccessModel.Value, cancellationToken);
        compartment.OnFailure(_ => 
            _logger.LogError(
                "A compartment with compartmentId: {Id} for user with Id: {UserId} not found", id,
                user.UserAccountId));
            
        return compartment.IsFailure
            ? Maybe<CompartmentModel>.None
            : Maybe<CompartmentModel>.From(ModelMapping.ToCompartmentModel(compartment.Value));
    }

    /// <summary>
    /// Check is a property profile with a given id exists and belongs to the current user
    /// </summary>
    /// <param name="user">An application user</param>
    /// <param name="propertyProfileId">A property profile id</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns></returns>
    public async Task<Result<PropertyProfileModel, ErrorDetails>> VerifyUserPropertyProfileAsync(
        ExternalApplicant user,
        Guid propertyProfileId,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(user);

        var userAccessModel = await GetUserAccessModelAsync(user, cancellationToken);

        return await _getPropertyProfilesService.GetPropertyByIdAsync(
            propertyProfileId, 
            userAccessModel.Value,
            cancellationToken)
        .OnFailure(_ =>
            _logger.LogError(
                "A compartment with propertyProfileId: {PropertyProfileId} not found for user with Id: {UserId} not found",
                propertyProfileId, user.UserAccountId))
            .MapError(_ => new ErrorDetails(ErrorTypes.NotFound))
            .Map(ModelMapping.ToPropertyProfileModel);
    }

    private async Task HandleCompartmentError(
        CompartmentModel compartmentModel, 
        ExternalApplicant user,
        string eventName, 
        UserDbErrorReason errorReason, 
        CancellationToken cancellationToken)
    {
        var error = errorReason == UserDbErrorReason.NotUnique
            ? $"A Compartment with the number: {compartmentModel.CompartmentNumber} already exists"
            : $"An error occurred  during {eventName}";
        var auditEvent = new AuditEvent(eventName, null, user.UserAccountId, _requestContext,
            new
            {
                compartmentModel.PropertyProfileId,
                compartmentModel.CompartmentNumber,
                compartmentModel.SubCompartmentName,
                compartmentModel.Designation,
                compartmentModel.GISData,
                compartmentModel.TotalHectares,
                Error = error
            });
        await _auditService.PublishAuditEventAsync(auditEvent, cancellationToken);
    }

    private async Task HandleCompartmentError(ImportCompartmentModel compartmentModel, Guid propertyProfileId, ExternalApplicant user,
      string eventName, UserDbErrorReason errorReason, CancellationToken cancellationToken)
    {
        var error = errorReason == UserDbErrorReason.NotUnique
            ? $"A Compartment with the number: {compartmentModel.CompartmentNumber} already exists"
            : $"An error occurred  during {eventName}";
        var auditEvent = new AuditEvent(eventName, null, user.UserAccountId, _requestContext,
            new
            {
                PropertyProfileId= propertyProfileId,
                compartmentModel.CompartmentNumber,
                compartmentModel.SubCompartmentName,
                compartmentModel.WoodlandName,
                compartmentModel.Designation,
                compartmentModel.GISData,
                compartmentModel.TotalHectares,
                Error = error
            });
        await _auditService.PublishAuditEventAsync(auditEvent, cancellationToken);
    }

    private async Task<Result<UserAccessModel>> GetUserAccessModelAsync(
        ExternalApplicant user, 
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(user);

        return await _retrieveUserAccountsService
            .RetrieveUserAccessAsync(user.UserAccountId!.Value, cancellationToken)
            .ConfigureAwait(false);
    }
}