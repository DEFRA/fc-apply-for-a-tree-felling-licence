using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.PublicRegister;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.PublicRegister;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Query;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.Gis.Models.Internal.Request;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace Forestry.Flo.Services.Gis.Services
{
    public class PublicRegister : BaseServices, Interfaces.IPublicRegister
    {
        private readonly PublicRegistryConfig _config;
        private readonly ILogger<PublicRegister> _logger;

        public PublicRegister(
            EsriConfig config, 
            IHttpClientFactory httpClientFactory, 
            ILogger<PublicRegister> logger)
        : base(httpClientFactory, "LandRegister", logger)
        {
            Guard.Against.Null(config.PublicRegister, "Public Registry Settings not configured");
            Guard.Against.Null(config.PublicRegister.LookUps);
            _config = config.PublicRegister;
            _logger = logger;

            TokenRequest = new GetTokenParameters(_config.GenerateTokenService.Username, _config.GenerateTokenService.Password);
            GetTokenPath = $"{_config.BaseUrl}{_config.GenerateTokenService.Path}";
        }

        ///<inheritdoc/>
        public async Task<Result<List<EsriCaseComments>>> GetCaseCommentsByCaseReferenceAsync(string caseReference, CancellationToken cancellationToken)
        {
            using (_logger.BeginScope(new Dictionary<string, object>
                   {
                       ["CorrelationId"] = Guid.NewGuid(),
                       ["CaseReference"] = caseReference
                   }))
            {
                _logger.LogInformation("GetCaseCommentsByCaseReferenceAsync called with caseReference: {CaseReference}", caseReference);

                var query = new QueryFeatureServiceParameters()
                {
                    WhereString = $"case_reference  = '{caseReference}'"
                };
                var path = $"{_config.BaseUrl}{_config.Comments.Path}/query";
                var result = await PostQueryWithConversionAsync<BaseQueryResponse<EsriCaseComments>>(query, path, _config.NeedsToken, cancellationToken);

                if (result.IsFailure)
                {
                    _logger.LogError("Failed to get case comments for caseReference {CaseReference}: {Error}", caseReference, result.Error);
                    return Result.Failure<List<EsriCaseComments>>(result.Error);
                }

                var comments = result.Value.Results.Select(r => r.Record).ToList();
                _logger.LogInformation("Retrieved {Count} comments for caseReference {CaseReference}", comments.Count, caseReference);
                return Result.Success(comments);
            }
        }

        ///<inheritdoc/>
        public async Task<Result<List<EsriCaseComments>>> GetCaseCommentsByCaseReferenceAndDateAsync(string caseReference, DateOnly date, CancellationToken cancellationToken)
        {
            using (_logger.BeginScope(new Dictionary<string, object>
                   {
                       ["CorrelationId"] = Guid.NewGuid(),
                       ["CaseReference"] = caseReference,
                       ["Date"] = date
                   }))
            {
                _logger.LogInformation("GetCaseCommentsByCaseReferenceAndDateAsync called with caseReference: {CaseReference}, date: {Date}", caseReference, date);

                var query = new QueryFeatureServiceParameters()
                {
                    WhereString = $"case_reference  = '{caseReference}' AND BETWEEN {date.ToString("yyyy-MM-dd") + " 00:00:00"} AND {date.ToString("yyyy-MM-dd") + " 23:59:59"}",
                };
                var path = $"{_config.BaseUrl}{_config.Comments.Path}/query";
                var result = await PostQueryWithConversionAsync<BaseQueryResponse<EsriCaseComments>>(query, path, _config.NeedsToken, cancellationToken);

                if (result.IsFailure)
                {
                    _logger.LogError("Failed to get case comments for caseReference {CaseReference} and date {Date}: {Error}", caseReference, date, result.Error);
                    return Result.Failure<List<EsriCaseComments>>(result.Error);
                }

                var comments = result.Value.Results.Select(r => r.Record).ToList();
                _logger.LogInformation("Retrieved {Count} comments for caseReference {CaseReference} and date {Date}", comments.Count, caseReference, date);
                return Result.Success(comments);
            }
        }

        ///<inheritdoc/>
        public async Task<Result<int>> AddCaseToConsultationRegisterAsync(
            AddToPublicRegisterModel dataModel, 
            CancellationToken cancellationToken)
        {
            using (_logger.BeginScope(new Dictionary<string, object>
                   {
                       ["CorrelationId"] = Guid.NewGuid(),
                       ["CaseReference"] = dataModel.CaseReference
                   }))
            {
                _logger.LogInformation("AddCaseToConsultationRegisterAsync called for caseRef: {CaseRef}", dataModel.CaseReference);

                if ((dataModel.Compartments?.Count ?? 0) == 0)
                {
                    _logger.LogError("No compartments set for application having reference {CaseReference}", dataModel.CaseReference);
                    return Result.Failure<int>("No compartments Set");
                }

                var geometryResult = ShapeHelper.MakeMultiPart(dataModel.Compartments.Select(c => c.ShapeGeometry).ToList());
                if (geometryResult.IsFailure)
                {
                    _logger.LogError("Unable to create a multipart geometry for the compartments for application having reference {CaseReference} with error {Error}", dataModel.CaseReference, geometryResult.Error);
                    return Result.Failure<int>(geometryResult.Error);
                }

                var caseObj = new BaseFeatureWithGeometryObject<Polygon, ForesterBoundaryCase<int>>
                {
                    GeometryObject = geometryResult.Value,
                    Attributes = new ForesterBoundaryCase<int>
                    {
                        ObjectId = dataModel.ExistingEsriId.HasValue ? dataModel.ExistingEsriId.Value : default,
                        AdminLocation = dataModel.AdminRegion,
                        CaseReference = dataModel.CaseReference,
                        CaseType = dataModel.CaseType,
                        GridReference = dataModel.GridReference,
                        LocalAuthority = dataModel.LocalAuthority,
                        NearestTown = dataModel.NearestTown,
                        PropertyName = dataModel.PropertyName,
                        TimeOnThePublicRegister = dataModel.Period,
                        TotalArea = dataModel.TotalArea,
                        PRStartDate = dataModel.PublicRegisterStart,
                        PREndDate = dataModel.PublicRegisterStart.Add(TimeSpan.FromDays(dataModel.Period)),
                        CaseStatus = _config.LookUps.Status.Consultation,
                        OnThePR = "1"
                    }
                };

                var compartmentFeatures = dataModel.Compartments.Select(c => new BaseFeatureWithGeometryObject<Polygon, ForesterCompartment<int>>
                {
                    GeometryObject = c.ShapeGeometry,
                    Attributes = new ForesterCompartment<int>
                    {
                        CaseReference = dataModel.CaseReference,
                        CompartmentLabel = c.CompartmentLabel,
                        Status = caseObj.Attributes.CaseStatus!,
                        CompartmentNumber = c.CompartmentNumber,
                        ONPublicRegister = caseObj.Attributes.OnThePR,
                        PublicRegStartDate = caseObj.Attributes.PRStartDate,
                        SubCompartmentNo = c.SubCompartmentNo,
                    }
                }).ToList();

                return await AddOrUpdateCaseOnPublicRegister(
                    dataModel.ExistingEsriId, dataModel.CaseReference, caseObj, compartmentFeatures, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public async Task<Result> RemoveCaseFromConsultationRegisterAsync(int objectId, string caseReference, DateTime endDateOnPR, CancellationToken cancellationToken)
        {
            using (_logger.BeginScope(new Dictionary<string, object>
                   {
                       ["CorrelationId"] = Guid.NewGuid(),
                       ["CaseReference"] = caseReference,
                       ["ObjectId"] = objectId
                   }))
            {
                _logger.LogInformation("RemoveCaseFromConsultationRegisterAsync called for caseReference: {CaseReference}, objectId: {ObjectId}", caseReference, objectId);

                if (objectId == 0)
                {
                    _logger.LogError("Object ID not correctly set for application having reference {CaseReference}", caseReference);
                    return Result.Failure("Object ID not correctly set");
                }

                if (string.IsNullOrEmpty(caseReference))
                {
                    _logger.LogError("No Case Reference given for application to remove from the consultation public register");
                    return Result.Failure("No Case Reference given");
                }

                // double-check the case is still on the PR
                var checkCaseStillOnPr = await CheckIfCaseStillOnPublicRegisterAsync(
                    objectId, caseReference, cancellationToken).ConfigureAwait(false);

                if (checkCaseStillOnPr.IsFailure)
                {
                    _logger.LogError("Unable to determine if case still exists on the PR for application having reference {CaseReference} with error {Error}", caseReference, checkCaseStillOnPr.Error);
                    return Result.Failure(checkCaseStillOnPr.Error);
                }
                if (checkCaseStillOnPr.Value == false)
                {
                    _logger.LogWarning("Case with reference {CaseReference} and ObjectId {ObjectId} was expected to be on the public register but was not found.", caseReference, objectId);
                    return Result.Success();
                }

                var boundaryCase = new List<ForesterBoundaryCase<int>>
                {
                    new ForesterBoundaryCase<int>
                    {
                        ObjectId = objectId,
                        CaseStatus = _config.LookUps.Status.FinalProposal,
                        PREndDate = endDateOnPR
                    }
                };
                var featuresJson = JsonConvert.SerializeObject(boundaryCase.First());
                var query = new EditFeaturesParameter($"[{{\"attributes\":{featuresJson}}}]");
                var path = $"{_config.BaseUrl}{_config.Boundaries.Path}/updateFeatures";

                var boundaryResult = await PostQueryWithConversionAsync<CreateUpdateDeleteResponse<int>>(query, path, _config.NeedsToken, cancellationToken);

                if (boundaryResult.IsFailure)
                {
                    _logger.LogError("Attempting to remove application reference {CaseRef} from consultation public register failed with error {Error}", caseReference, boundaryResult.Error);
                    return Result.Failure(boundaryResult.Error);
                }

                var compartmentUpdateResult = await UpdateCompartmentsByCaseRefAsync(caseReference, compartmentStatus: _config.LookUps.Status.FinalProposal, cancellationToken: cancellationToken);

                if (compartmentUpdateResult.IsFailure)
                {
                    _logger.LogError("Attempting to update compartments for application reference {CaseRef} on the Consultation Public Register failed with error {Error}",
                        caseReference, compartmentUpdateResult.Error);
                }

                return compartmentUpdateResult.IsFailure ? Result.Failure($"Updated the Boundary, Updating Compartments returned '{compartmentUpdateResult.Error}'")
                    : Result.Success();
            }
        }

        /// <inheritdoc/>
        public async Task<Result<int>> AddCaseToDecisionRegisterAsync(
            AddToDecisionPublicRegisterModel dataModel, 
            CancellationToken cancellationToken)
        {
            using (_logger.BeginScope(new Dictionary<string, object>
                   {
                       ["CorrelationId"] = Guid.NewGuid(),
                       ["CaseReference"] = dataModel.CaseReference
                   }))
            {
                _logger.LogInformation("AddCaseToDecisionRegisterAsync called for caseReference: {CaseReference}, objectId: {ObjectId}", dataModel.CaseReference, dataModel.ExistingEsriId);

                if ((dataModel.Compartments?.Count ?? 0) == 0)
                {
                    _logger.LogError("No compartments set for application having reference {CaseReference}", dataModel.CaseReference);
                    return Result.Failure<int>("No compartments Set");
                }

                var caseStatus = dataModel.FellingLicenceOutcome.ToLowerInvariant() switch
                {
                    "approved" => "Approved",
                    "refused" => "Refused",
                    "referredtolocalauthority" => "Referred to LA",
                    _ => null
                };

                if (caseStatus == null)
                {
                    _logger.LogError("Felling application outcome of {FellingLicenceOutcome} on application having reference {CaseReference} is invalid for sending to the Decision Public Register", dataModel.FellingLicenceOutcome, dataModel.CaseReference);
                    return Result.Failure<int>($"Felling application outcome of {dataModel.FellingLicenceOutcome} on application having reference {dataModel.CaseReference} is invalid for sending to the Decision Public Register");
                }

                var geometryResult = ShapeHelper.MakeMultiPart(dataModel.Compartments.Select(c => c.ShapeGeometry).ToList());
                if (geometryResult.IsFailure)
                {
                    _logger.LogError("Unable to create a multipart geometry for the compartments for application having reference {CaseReference} with error {Error}", dataModel.CaseReference, geometryResult.Error);
                    return Result.Failure<int>(geometryResult.Error);
                }

                var caseObj = new BaseFeatureWithGeometryObject<Polygon, ForesterBoundaryCase<int>>
                {
                    GeometryObject = geometryResult.Value,
                    Attributes = new ForesterBoundaryCase<int>
                    {
                        ObjectId = dataModel.ExistingEsriId.HasValue ? dataModel.ExistingEsriId.Value : default,
                        AdminLocation = dataModel.AdminRegion,
                        CaseReference = dataModel.CaseReference,
                        CaseType = dataModel.CaseType,
                        GridReference = dataModel.GridReference,
                        LocalAuthority = dataModel.LocalAuthority,
                        NearestTown = dataModel.NearestTown,
                        PropertyName = dataModel.PropertyName,
                        TotalArea = dataModel.TotalArea,
                        PRStartDate = dataModel.PublicRegisterStart,
                        PREndDate = dataModel.PublicRegisterStart.Add(TimeSpan.FromDays(dataModel.Period)),
                        TimeOnThePublicRegister = dataModel.Period,
                        CaseStatus = caseStatus,
                        CaseApprovalDate = dataModel.CaseApprovalDate,
                        OnThePR = "1"
                    }
                };

                var compartmentFeatures = dataModel.Compartments.Select(c => new BaseFeatureWithGeometryObject<Polygon, ForesterCompartment<int>>
                {
                    GeometryObject = c.ShapeGeometry,
                    Attributes = new ForesterCompartment<int>
                    {
                        CaseReference = dataModel.CaseReference,
                        CompartmentLabel = c.CompartmentLabel,
                        Status = caseStatus,
                        CompartmentNumber = c.CompartmentNumber,
                        ONPublicRegister = caseObj.Attributes.OnThePR,
                        PublicRegStartDate = caseObj.Attributes.PRStartDate,
                        SubCompartmentNo = c.SubCompartmentNo,
                    }
                }).ToList();

                return await AddOrUpdateCaseOnPublicRegister(
                    dataModel.ExistingEsriId,
                    dataModel.CaseReference,
                    caseObj,
                    compartmentFeatures, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public async Task<Result> RemoveCaseFromDecisionRegisterAsync(int objectId, string caseReference, CancellationToken cancellationToken)
        {
            using (_logger.BeginScope(new Dictionary<string, object>
                   {
                       ["CorrelationId"] = Guid.NewGuid(),
                       ["CaseReference"] = caseReference,
                       ["ObjectId"] = objectId
                   }))
            {
                _logger.LogInformation("RemoveCaseFromDecisionRegisterAsync called for caseReference: {CaseReference}, objectId: {ObjectId}", caseReference, objectId);

                if (objectId == 0)
                {
                    _logger.LogError("Object ID not correctly set for application having reference {CaseReference}", caseReference);
                    return Result.Failure("Object ID not correctly set");
                }

                if (string.IsNullOrEmpty(caseReference))
                {
                    _logger.LogError("No Case Reference given for application to remove from the decision public register");
                    return Result.Failure("No Case Reference given");
                }

                // double-check the case is still on the PR
                var checkCaseStillOnPr = await CheckIfCaseStillOnPublicRegisterAsync(
                    objectId, caseReference, cancellationToken).ConfigureAwait(false);

                if (checkCaseStillOnPr.IsFailure)
                {
                    _logger.LogError("Unable to determine if case still exists on the PR for application having reference {CaseReference} with error {Error}", caseReference, checkCaseStillOnPr.Error);
                    return Result.Failure(checkCaseStillOnPr.Error);
                }
                if (checkCaseStillOnPr.Value == false)
                {
                    _logger.LogWarning("Case with reference {CaseReference} and ObjectId {ObjectId} was expected to be on the public register but was not found.", caseReference, objectId);
                    return Result.Success();
                }

                var boundaryCase = new List<ForesterBoundaryCase<int>>
                {
                    new ForesterBoundaryCase<int>
                    {
                        ObjectId = objectId,
                        OnThePR = "0",
                        CaseStatus = _config.LookUps.Status.FinalProposal
                    }
                };

                var query = new EditFeaturesParameter($"{JsonConvert.SerializeObject(boundaryCase)}");
                var path = $"{_config.BaseUrl}{_config.Boundaries.Path}/updateFeatures";

                var boundaryResult = await PostQueryWithConversionAsync<CreateUpdateDeleteResponse<int>>(query, path, _config.NeedsToken, cancellationToken);

                if (boundaryResult.IsFailure)
                {
                    _logger.LogError("Attempting to remove application reference {CaseRef} from decision public register failed with error {Error}", caseReference, boundaryResult.Error);
                    return Result.Failure(boundaryResult.Error);
                }

                var compartmentUpdateResult = await UpdateCompartmentsByCaseRefAsync(caseReference, onPublicRegister: "0", compartmentStatus: _config.LookUps.Status.FinalProposal, cancellationToken: cancellationToken);

                if (compartmentUpdateResult.IsFailure)
                {
                    _logger.LogError("Attempting to update compartments for application reference {CaseRef} on the Decision Public Register failed with error {Error}",
                        caseReference, compartmentUpdateResult.Error);
                }

                return compartmentUpdateResult.IsFailure ? Result.Failure($"Updated the Boundary, Updating Compartments returned '{compartmentUpdateResult.Error}'")
                    : Result.Success();
            }
        }

        private async Task<Result<int>> AddOrUpdateCaseOnPublicRegister(
            int? existingObjectId,
            string caseReference,
            BaseFeatureWithGeometryObject<Polygon, ForesterBoundaryCase<int>> caseObject,
            List<BaseFeatureWithGeometryObject<Polygon, ForesterCompartment<int>>> compartmentFeatures,
            CancellationToken cancellationToken)
        {
            if (existingObjectId.HasValue)
            {
                // double-check the case is still on the PR

                var checkCaseStillOnPr = await CheckIfCaseStillOnPublicRegisterAsync(
                    existingObjectId.Value, caseReference, cancellationToken).ConfigureAwait(false);

                if (checkCaseStillOnPr.IsFailure)
                {
                    _logger.LogError("Unable to determine if case still exists on the PR for application having reference {CaseReference} with error {Error}", caseReference, checkCaseStillOnPr.Error);
                    return Result.Failure<int>(checkCaseStillOnPr.Error);
                }
                if (checkCaseStillOnPr.Value == false)
                {
                    _logger.LogWarning("Case with reference {CaseReference} and ObjectId {ObjectId} was expected to be on the public register but was not found. Attempting to add as new case.", caseReference, existingObjectId.Value);
                    existingObjectId = null;
                    caseObject.Attributes.ObjectId = default;
                }
            }

            // delete compartment features first, before updating boundary layer and then re-adding compartment features
            if (existingObjectId.HasValue)
            {
                var deleteExistingCompartments = await DeleteCompartmentsByCaseRefAsync(caseReference, cancellationToken);
                if (deleteExistingCompartments.IsFailure)
                {
                    _logger.LogError("Unable to remove existing compartment features for application having reference {CaseReference} with error {Error}", caseReference, deleteExistingCompartments.Error);
                    return Result.Failure<int>(deleteExistingCompartments.Error);
                }
            }

            var objToAdd = new EditFeaturesParameter($"{JsonConvert.SerializeObject(new List<BaseFeatureWithGeometryObject<Polygon, ForesterBoundaryCase<int>>> { caseObject })}");

            var path = existingObjectId.HasValue
                ? $"{_config.BaseUrl}{_config.Boundaries.Path}/updateFeatures"
                : $"{_config.BaseUrl}{_config.Boundaries.Path}/addFeatures";

            var result = await PostQueryWithConversionAsync<CreateUpdateDeleteResponse<int>>(objToAdd, path, _config.NeedsToken, cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogError("Attempting to add application reference {CaseRef} to public register failed with error {Error}", caseReference, result.Error);
                return Result.Failure<int>(result.Error);
            }

            int objectId;

            if (existingObjectId.HasNoValue())
            {
                if (result.Value.AddResults == null)
                {
                    _logger.LogError("Attempting to add application reference {CaseRef} to public register returned no results", caseReference);
                    return Result.Failure<int>("Failed to add application to PR boundary layer");
                }

                var errorResults = result.Value.AddResults
                    .Where(r => r.ErrorDetails != null)
                    .Select(e => e.ErrorDetails)
                    .ToList();

                if (errorResults.Any())
                {
                    _logger.LogError("Attempting to add application reference {CaseRef} to public register returned errors: {Errors}", caseReference, string.Join(", ", errorResults.Select(r => r!.Details)));
                    return Result.Failure<int>(string.Join(", ", errorResults.Select(r => r!.Details)));
                }

                objectId = result.Value.AddResults!.First(r => r.WasSuccessful).ObjectId;
            }
            else
            {
                if (result.Value.UpdateResults == null)
                {
                    _logger.LogError("Attempting to update application reference {CaseRef} with id {ObjectId} on public register returned no results", caseReference, existingObjectId);
                    return Result.Failure<int>("Failed to update application on PR boundary layer");
                }

                var errorResults = result.Value.UpdateResults
                    .Where(r => r.ErrorDetails != null)
                    .Select(e => e.ErrorDetails)
                    .ToList();

                if (errorResults.Any())
                {
                    _logger.LogError("Attempting to update application reference {CaseRef} with id {ObjectId} on public register returned errors: {Errors}", caseReference, existingObjectId!.Value, string.Join(", ", errorResults.Select(r => r!.Details)));
                    return Result.Failure<int>(string.Join(", ", errorResults.Select(r => r!.Details)));
                }

                objectId = existingObjectId!.Value;
            }

            var compartmentResult = await AddCompartmentsToCaseAsync(compartmentFeatures, cancellationToken);

            if (!compartmentResult.IsFailure)
            {
                _logger.LogInformation("Successfully added/updated case {CaseRef} to public register with ObjectId {ObjectId}", caseReference, objectId);
                return Result.Success(objectId);
            }

            var deleteResult = await DeleteBoundariesAsync(objectId, cancellationToken);

            if (deleteResult.IsFailure)
            {
                _logger.LogError("Attempting to roll back boundaries for application with reference {CaseRef} from public register failed with error {Error}", caseReference, deleteResult.Error);
            }

            return Result.Failure<int>(deleteResult.IsFailure
                ? "Added Case Boundary, but failed to add compartments. Unable to rollback Boundary"
                : "Added Case Boundary, but failed to add compartments. Boundary has been rolled back");
        }

        private async Task<Result> UpdateCompartmentsByCaseRefAsync(string caseReference, CancellationToken cancellationToken, string? compartmentStatus = null, string? onPublicRegister = null)
        {
            using (_logger.BeginScope(new Dictionary<string, object>
                   {
                       ["CorrelationId"] = Guid.NewGuid(),
                       ["CaseReference"] = caseReference
                   }))
            {
                _logger.LogInformation("UpdateCompartmentsByCaseRefAsync called for caseReference: {CaseReference}, status: {CompartmentStatus}, onPublicRegister: {OnPublicRegister}", caseReference, compartmentStatus, onPublicRegister);

                var basePath = $"{_config.BaseUrl}{_config.Compartments.Path}";
                var path = $"{_config.BaseUrl}{_config.Compartments.Path}/updateFeatures";

                var getIDs = await GetEsriIDs_ByFieldAsync<int>(caseReference, "case_reference", basePath, cancellationToken);

                if (getIDs.IsFailure)
                {
                    _logger.LogError("Unable to query the compartments layer for case reference {CaseReference} with error {Error}", caseReference, getIDs.Error);
                    return Result.Failure("Unable to query the compartments layer");
                }

                var features = getIDs.Value.Select(x => new ForesterCompartment<int>
                {
                    ObjectId = x,
                    Status = compartmentStatus,
                    ONPublicRegister = onPublicRegister
                }).ToList();

                var compartmentsForUpdateJson = new StringBuilder();

                foreach (var featuresJson in features.Select(JsonConvert.SerializeObject))
                {
                    compartmentsForUpdateJson.Append($"{{\"attributes\":{featuresJson}}},");
                }

                var query = new EditFeaturesParameter($"[{compartmentsForUpdateJson}]");

                var compartmentResult = await PostQueryWithConversionAsync<CreateUpdateDeleteResponse<int>>(query, path, _config.NeedsToken, cancellationToken);

                if (compartmentResult.IsFailure)
                {
                    _logger.LogError("Attempting to update compartments for case reference {CaseReference} failed with error {Error}", caseReference, compartmentResult.Error);
                    return Result.Failure(compartmentResult.Error);
                }

                if (compartmentResult.Value.UpdateResults == null)
                {
                    _logger.LogError("Attempting to update compartments for case reference {CaseReference} returned no results", caseReference);
                    return Result.Failure<int>("No Results");
                }

                var errorResults = compartmentResult.Value.UpdateResults.Where(r => r.ErrorDetails != null).Select(e => e.ErrorDetails);

                if (errorResults.Any())
                {
                    _logger.LogError("Update compartments for case reference {CaseReference} returned errors: {Errors}", caseReference, string.Join(", ", errorResults.Select(r => r!.Details)));
                    return Result.Failure<int>(string.Join(", ", errorResults.Select(r => r!.Details)));
                }

                _logger.LogInformation("Compartments updated successfully for caseReference: {CaseReference}", caseReference);
                return Result.Success();
            }
        }

        private async Task<Result> AddCompartmentsToCaseAsync(List<BaseFeatureWithGeometryObject<Polygon, ForesterCompartment<int>>> compartmentFeatures, CancellationToken cancellationToken)
        {
            using (_logger.BeginScope(new Dictionary<string, object>
                   {
                       ["CorrelationId"] = Guid.NewGuid()
                   }))
            {
                _logger.LogInformation("AddCompartmentsToCaseAsync called with {Count} compartments", compartmentFeatures.Count);

                var objToAdd = new EditFeaturesParameter($"{JsonConvert.SerializeObject(compartmentFeatures)}");

                var path = $"{_config.BaseUrl}{_config.Compartments.Path}/addFeatures";
                var result = await PostQueryWithConversionAsync<CreateUpdateDeleteResponse<int>>(objToAdd, path, _config.NeedsToken, cancellationToken);

                if (result.IsFailure)
                {
                    _logger.LogError("Attempting to add compartments to the case failed with error {Error}", result.Error);
                    return Result.Failure<int>(result.Error);
                }

                if (result.Value.AddResults == null)
                {
                    _logger.LogError("Attempting to add compartments to the case returned no results");
                    return Result.Failure<int>("No Results");
                }

                var errorResults = result.Value.AddResults.Where(r => r.ErrorDetails != null).Select(e => e.ErrorDetails);

                if (errorResults.Any())
                {
                    _logger.LogError("Add compartments to case returned errors: {Errors}", string.Join(", ", errorResults.Select(r => r!.Details)));
                    return Result.Failure(string.Join(", ", errorResults.Select(r => r!.Details)));
                }

                _logger.LogInformation("Compartments added to case successfully.");
                return Result.Success();
            }
        }

        private async Task<Result> DeleteBoundariesAsync(int id, CancellationToken cancellationToken)
        {
            using (_logger.BeginScope(new Dictionary<string, object>
                   {
                       ["CorrelationId"] = Guid.NewGuid(),
                       ["BoundaryId"] = id
                   }))
            {
                _logger.LogInformation("DeleteBoundariesAsync called for boundary id: {BoundaryId}", id);

                var body = new DeleteFeatureParameter(new List<int> { id });

                var path = $"{_config.BaseUrl}{_config.Boundaries.Path}/deleteFeatures";

                var result = await PostQueryWithConversionAsync<WasSuccessful>(body, path, _config.NeedsToken, cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Boundary {BoundaryId} deleted successfully.", id);
                    return Result.Success();
                }
                else
                {
                    _logger.LogError("Failed to delete boundary {BoundaryId}: {Error}", id, result.Error);
                    return Result.Failure(result.Error);
                }
            }
        }

        private async Task<Result> DeleteCompartmentsByCaseRefAsync(string caseReference, CancellationToken cancellationToken)
        {
            using (_logger.BeginScope(new Dictionary<string, object>
                   {
                       ["CorrelationId"] = Guid.NewGuid(),
                       ["CaseReference"] = caseReference
                   }))
            {
                _logger.LogInformation("DeleteCompartmentsByCaseRefAsync called for caseReference: {CaseReference}", caseReference);

                var basePath = $"{_config.BaseUrl}{_config.Compartments.Path}";

                var getIDs = await GetEsriIDs_ByFieldAsync<int>(caseReference, "case_reference", basePath, cancellationToken);

                if (getIDs.IsFailure)
                {
                    _logger.LogError("Unable to query the compartments layer for case reference {CaseReference} with error {Error}", caseReference, getIDs.Error);
                    return Result.Failure("Unable to query the compartments layer");
                }

                var compartmentIds = getIDs.Value;

                if (!compartmentIds.Any())
                {
                    _logger.LogDebug("No compartment objects found for case reference {CaseReference}", caseReference);
                    return Result.Success();
                }

                var body = new DeleteFeatureParameter(compartmentIds);

                var path = $"{_config.BaseUrl}{_config.Compartments.Path}/deleteFeatures";

                var result = await PostQueryWithConversionAsync<WasSuccessful>(body, path, _config.NeedsToken, cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Compartments for case reference {CaseReference} deleted successfully.", caseReference);
                    return Result.Success();
                }

                _logger.LogError("Failed to delete Compartments for case reference {CaseReference}: {Error}", caseReference, result.Error);
                return Result.Failure("Unable to delete existing compartments");
            }
        }

        private async Task<Result<bool>> CheckIfCaseStillOnPublicRegisterAsync(
            int objectId,
            string caseReference,
            CancellationToken cancellationToken)
        {
            var basePath = $"{_config.BaseUrl}{_config.Boundaries.Path}";

            var getIDs = await GetEsriIDs_ByFieldAsync<int>(caseReference, "case_reference", basePath, cancellationToken);

            if (getIDs.IsFailure)
            {
                _logger.LogError("Unable to query the boundaries layer for case reference {CaseReference} with error {Error}", caseReference, getIDs.Error);
                return Result.Failure<bool>("Unable to query the boundaries layer");
            }

            var caseIds = getIDs.Value;

            return Result.Success(caseIds.Contains(objectId));
        }
    }
}