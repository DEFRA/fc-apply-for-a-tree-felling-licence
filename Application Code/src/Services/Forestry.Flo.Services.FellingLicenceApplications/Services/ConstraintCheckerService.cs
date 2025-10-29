using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forestry.Flo.Services.FellingLicenceApplications.Services;

public class ConstraintCheckerService
{
    private readonly ILandInformationSearch _landInformationSearch;
    private readonly LandInformationSearchOptions _landInformationSearchOptions;
    private readonly IFellingLicenceApplicationInternalRepository _fellingLicenceApplicationInternalRepository;
    private readonly IPropertyProfileRepository _propertyProfileRepository;
    private readonly IAuditService<ConstraintCheckerService> _auditService;
    private readonly RequestContext _requestContext;
    private readonly ILogger<ConstraintCheckerService> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="ConstraintCheckerService"/>.
    /// </summary>
    /// <param name="landInformationSearch"></param>
    /// <param name="landInformationSearchOptions"></param>
    /// <param name="fellingLicenceApplicationInternalRepository"></param>
    /// <param name="propertyProfileRepository"></param>
    /// <param name="auditService"></param>
    /// <param name="requestContext"></param>
    /// <param name="logger"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public ConstraintCheckerService(
        ILandInformationSearch landInformationSearch,
        IOptions<LandInformationSearchOptions> landInformationSearchOptions,
        IFellingLicenceApplicationInternalRepository fellingLicenceApplicationInternalRepository,
        IPropertyProfileRepository propertyProfileRepository,
        IAuditService<ConstraintCheckerService> auditService,
        RequestContext requestContext,
        ILogger<ConstraintCheckerService> logger)
    {
        _landInformationSearch = Guard.Against.Null(landInformationSearch);
        _landInformationSearchOptions = Guard.Against.Null(landInformationSearchOptions.Value);
        _fellingLicenceApplicationInternalRepository = Guard.Against.Null(fellingLicenceApplicationInternalRepository);
        _propertyProfileRepository = Guard.Against.Null(propertyProfileRepository);
        _requestContext = Guard.Against.Null(requestContext);
        _auditService = Guard.Against.Null(auditService);
        _logger = logger;
        
    }

    /// <summary>
    /// Despite the name of this (reflecting the users intent), it actually causes
    /// the sending of compartment geometry for the FLA to the LIS Feature service feature layer,
    /// and then on success crafts the URL to the FC internal LIS or public facing LiS - which is a deep link
    /// into the relevant Forester web for the specified Felling Application. 
    /// </summary>
    public async Task<Result<Uri>> ExecuteAsync(
        ConstraintCheckRequest constraintCheckRequest,
        CancellationToken cancellationToken)
    {
        var (isFound, fellingLicenceApplication) = await _fellingLicenceApplicationInternalRepository
            .GetAsync(constraintCheckRequest.ApplicationId, cancellationToken);
        
        string error;

        if (isFound)
        {
            _logger.LogDebug("FLA found having application Id of [{id}].", constraintCheckRequest.ApplicationId);
            
            try
            {
                IReadOnlyList<InternalCompartmentDetails<Polygon>> internalCompartments;

                if (constraintCheckRequest.IsInternalUser)
                {
                    
                    internalCompartments = MapSubmittedFlaCompartmentsToGisInternalCompartments(fellingLicenceApplication
                        .SubmittedFlaPropertyDetail!.SubmittedFlaPropertyCompartments!);
                }
                else
                {
                    //if request is from an applicant user, then we will need to get the compartment details in alternate way,
                    //as cannot get simply from the submitted compartments. Need to get all property compartments and pluck those which
                    //are included in the Proposed Felling Details;

                    var (isSuccess, _, propertyProfile, errorReason) = await _propertyProfileRepository.GetByIdAsync(
                        fellingLicenceApplication!.LinkedPropertyProfile!.PropertyProfileId, cancellationToken);

                    if (isSuccess)
                    {
                        var allPropertyCompartments = propertyProfile.Compartments;
                        var compartmentsIncludedInPreSubmittedApplication = fellingLicenceApplication.LinkedPropertyProfile.ProposedFellingDetails!
                            .Select(pfd => allPropertyCompartments.Single(x => x.Id == pfd.PropertyProfileCompartmentId))
                            .ToList();
                        internalCompartments = MapPreSubmittedFlaCompartmentsToGisInternalCompartments(compartmentsIncludedInPreSubmittedApplication);
                    }
                    else
                    {
                        error = $"Unable to proceed with LIS Constraint Check for application having id of [{constraintCheckRequest.ApplicationId}] " +
                                $"as could not get related property details having profile id of [{fellingLicenceApplication.LinkedPropertyProfile!.PropertyProfileId}] in order to retrieve compartments for the applicant's (non submitted) application." +
                                $"Error reason is [{errorReason}]";

                        _logger.LogError("Failed to get property profile id {PropertyProfileId} for LIS check of application {ApplicationId}, error: {Error}",
                            fellingLicenceApplication!.LinkedPropertyProfile!.PropertyProfileId, constraintCheckRequest.ApplicationId, errorReason);
                        return await HandleFailureAsync(constraintCheckRequest, error, cancellationToken);
                    }
                }
                
                var  hasClearedLayer = await _landInformationSearch.ClearLayerAsync(fellingLicenceApplication.Id.ToString(), cancellationToken);
                if (hasClearedLayer.IsFailure) {
                    error = $"Unable to proceed with LIS Constraint Check for application having id of [{constraintCheckRequest.ApplicationId}] " +
                            $"as could not clear the layer for the FLA having id of [{fellingLicenceApplication.Id}]. Error is [{hasClearedLayer.Error}].";
                    return await HandleFailureAsync(constraintCheckRequest, error, cancellationToken);
                }
                var result = await _landInformationSearch.AddFellingLicenceGeometriesAsync(fellingLicenceApplication.Id, internalCompartments, cancellationToken);

                if (result.IsSuccess)
                {
                    return await HandleSuccessAsync(constraintCheckRequest, result.Value, cancellationToken);
                }
                
                error = $"Unable to proceed with LIS Constraint Check for application having id of [{constraintCheckRequest.ApplicationId}] " +
                        $"as could not successfully send case geometries to LIS, received error back is [{result.Error}].";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Encountered error during the processing/sending of data to Land Information Search.");
                return await HandleFailureAsync(constraintCheckRequest, ex.Message, cancellationToken);
            }
        }
        else
        {
            error = $"An FLA was not found which had an application Id of [{constraintCheckRequest.ApplicationId}]. Unable to proceed with LIS Constraint Check.";
        }
        return await HandleFailureAsync(constraintCheckRequest, error, cancellationToken);
    }

    private IReadOnlyList<InternalCompartmentDetails<Polygon>> MapPreSubmittedFlaCompartmentsToGisInternalCompartments(
        ICollection<Compartment> compartments)
    {
        var compartmentCount = compartments.Count;
      
        var internalCompartmentDetailsList = new List<InternalCompartmentDetails<Polygon>>(compartmentCount);
        
        _logger.LogDebug("Building [{countOfPreSubmittedCompartments}] compartment geometry/attribute objects.", compartmentCount);

        internalCompartmentDetailsList.AddRange(compartments.Select(compartment => compartment.ToInternalCompartmentDetails()));

        _logger.LogDebug("Built compartment geometry objects.");
        
        return internalCompartmentDetailsList;
    }

    private IReadOnlyList<InternalCompartmentDetails<Polygon>> MapSubmittedFlaCompartmentsToGisInternalCompartments(ICollection<SubmittedFlaPropertyCompartment> submittedFlaPropertyCompartments)
    {
        var compartmentCount = submittedFlaPropertyCompartments.Count;
        
        var internalCompartmentDetailsList = new List<InternalCompartmentDetails<Polygon>>(compartmentCount);

        _logger.LogDebug("Building [{countOfSubmittedCompartments}] compartment geometry/attribute objects.", compartmentCount);

        internalCompartmentDetailsList.AddRange(submittedFlaPropertyCompartments.Select(submittedFlaPropertyCompartment => submittedFlaPropertyCompartment.ToInternalCompartmentDetails()));
      
        _logger.LogDebug("Built compartment geometry objects.");
        
        return internalCompartmentDetailsList;
    }

    private async Task<Result<Uri>> HandleSuccessAsync(ConstraintCheckRequest constraintCheckRequest,
        CreateUpdateDeleteResponse<int> createUpdateDeleteResponse,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Successfully sent case geometries to LIS for application having id of [{id}], " +
                         "proceeding with constructing deep-link URL for Constraint Check.", constraintCheckRequest.ApplicationId);

        var deepLinkQueryString = new Dictionary<string, string>
        {
            ["isFlo"] = "true", 
            ["caseId"] = constraintCheckRequest.ApplicationId.ToString(),
        };

        deepLinkQueryString.Add(constraintCheckRequest.IsInternalUser ? "lisconfig" : "config",
            _landInformationSearchOptions.LisConfig);

        var url = QueryHelpers.AddQueryString(_landInformationSearchOptions.DeepLinkUrlAndPath, deepLinkQueryString);

        var checkType = constraintCheckRequest.IsInternalUser ? "FC Internal User" : "External applicant";
        _logger.LogDebug("Built Url of [{url}] for the {checkType} LIS constraint report check for " +
                         "application having id of [{id}].", url, checkType, constraintCheckRequest.ApplicationId);

        await RaiseSuccessAuditEventAsync(
            constraintCheckRequest.UserAccountId, 
            constraintCheckRequest.ApplicationId, 
            createUpdateDeleteResponse, 
            url, 
            cancellationToken);
        return Result.Success(new Uri(url));
    }

    private async Task<Result<Uri>> HandleFailureAsync(
        ConstraintCheckRequest constraintCheckRequest,
        string error,
        CancellationToken cancellationToken)
    {
        await RaiseFailureAuditEventAsync(constraintCheckRequest.ApplicationId, constraintCheckRequest.ApplicationId, error, cancellationToken);
        return Result.Failure<Uri>(error);
    }

    private async Task RaiseSuccessAuditEventAsync(Guid? userAccountId,
        Guid applicationId,
        CreateUpdateDeleteResponse<int> createUpdateDeleteResponse,
        string url,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.ConstraintCheckerExecutionSuccess, applicationId, userAccountId, _requestContext,
            new
            {
                url,
                esriFeatureServiceResponse = createUpdateDeleteResponse
            }
        ), cancellationToken);
    }

    private async Task RaiseFailureAuditEventAsync(
        Guid? userAccountId,
        Guid applicationId,
        string error,
        CancellationToken cancellationToken)
    {
        await _auditService.PublishAuditEventAsync(new AuditEvent(
            AuditEvents.ConstraintCheckerExecutionFailure, applicationId, userAccountId, _requestContext,
            new
            {
                error
            }
        ), cancellationToken);
    }
}