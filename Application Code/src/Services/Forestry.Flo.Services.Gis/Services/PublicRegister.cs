﻿using System.Text;
using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis.Models.Esri.Configuration;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Form;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json;
using Forestry.Flo.Services.Gis.Models.Esri.RequestObjects.Json.PublicRegister;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.PublicRegister;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Query;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Forestry.Flo.Services.Gis.Services
{
    public class PublicRegister : BaseServices, Interfaces.IPublicRegister
    {
        private readonly PublicRegistryConfig _config;
        private readonly ILogger<PublicRegister> _logger;

        /// <summary>
        /// The Constructor for the class.
        /// Here we also prep the token set up as its likely that if you are using this class then you'll want to log into the AGOL Platform
        /// </summary>
        /// <param name="config">The ESRI config Options</param>
        /// <param name="httpClientFactory">The Factory for the http requests</param>
        /// <param name="logger">The Logger object</param>
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
            var query = new QueryFeatureServiceParameters()
            {
                WhereString = $"case_reference  = '{caseReference}'"
            };
            var path = $"{_config.BaseUrl}{_config.Comments.Path}/query";
            var result = await PostQueryWithConversionAsync<BaseQueryResponse<EsriCaseComments>>(query, path, _config.NeedsToken, cancellationToken);

            return result.IsFailure ? Result.Failure<List<EsriCaseComments>>(result.Error)
                : Result.Success(result.Value.Results.Select(r => r.Record).ToList());
        }

        ///<inheritdoc/>
        public async Task<Result<List<EsriCaseComments>>> GetCaseCommentsByCaseReferenceAndDateAsync(string caseReference, DateOnly date, CancellationToken cancellationToken)
        {
            var query = new QueryFeatureServiceParameters()
            {
                WhereString = $"case_reference  = '{caseReference}' AND BETWEEN {date.ToString("yyyy-MM-dd") + " 00:00:00"} AND {date.ToString("yyyy-MM-dd") + " 23:59:59"}",
            };
            var path = $"{_config.BaseUrl}{_config.Comments.Path}/query";
            var result = await PostQueryWithConversionAsync<BaseQueryResponse<EsriCaseComments>>(query, path, _config.NeedsToken, cancellationToken);

            return result.IsFailure ? Result.Failure<List<EsriCaseComments>>(result.Error)
                : Result.Success(result.Value.Results.Select(r => r.Record).ToList());
        }

        ///<inheritdoc/>
        public async Task<Result<int>> AddCaseToConsultationRegisterAsync(
            string caseRef, 
            string propertyName, 
            string caseType, 
            string gridRef, 
            string nearestTown, 
            string localAdminArea, 
            string adminRegion, 
            DateTime publicRegisterStart, 
            int period, 
            double? broadLeafArea, 
            double? coniferousArea, 
            double? openGroundArea, 
            double? totalArea, 
            List<InternalCompartmentDetails<Polygon>> compartments, 
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Attempting to publish application reference {CaseRef} to consultation public register", caseRef);

            if (compartments.Count == 0)
            {
                _logger.LogError("No compartments set for application having reference {CaseReference}", caseRef);
                return Result.Failure<int>("No compartments Set");
            }

            var geometryResult = ShapeHelper.MakeMultiPart(compartments.Select(c => c.ShapeGeometry).ToList());
            if (geometryResult.IsFailure)
            {
                _logger.LogError("Unable to create a multipart geometry for the compartments for application having reference {CaseReference} with error {Error}", caseRef, geometryResult.Error);
                return Result.Failure<int>(geometryResult.Error);
            }

            // TODO: Validate Model
            var caseObj = new BaseFeatureWithGeometryObject<Polygon, ForesterBoundaryCase<int>>
            {
                GeometryObject = geometryResult.Value,
                Attributes = new ForesterBoundaryCase<int>
                {
                    AdminLocation = adminRegion,
                    CaseReference = caseRef,
                    BroadLeafArea = broadLeafArea,
                    CaseType = caseType,
                    ConiferousArea = coniferousArea,
                    GridReference = gridRef,
                    LocalAuthority = localAdminArea,
                    NearestTown = nearestTown,
                    OpenGroundArea = openGroundArea,
                    PropertyName = propertyName,
                    TimeOnTheConsultationRegister = period,
                    TotalArea = totalArea,
                    PRStartDate = publicRegisterStart,
                    PREndDate = publicRegisterStart.Add(TimeSpan.FromDays(period)),
                    CaseStatus = _config.LookUps.Status.Consultation,
                    OnThePR = "1"
                }
            };

            var compartmentFeatures = compartments.Select(c => new BaseFeatureWithGeometryObject<Polygon, ForesterCompartment<int>>
            {
                GeometryObject = c.ShapeGeometry,
                Attributes = new ForesterCompartment<int>
                {
                    CaseReference = caseRef,
                    CompartmentLabel = c.CompartmentLabel,
                    Status = caseObj.Attributes.CaseStatus!,
                    CompartmentNumber = c.CompartmentNumber,
                    ONPublicRegister = caseObj.Attributes.OnThePR,
                    PublicRegStartDate = caseObj.Attributes.PRStartDate,
                    SubCompartmentNo = c.SubCompartmentNo,
                }
            }).ToList();



            // Features are actually an array, but to keep the base add method simple, we handle the logic here. 
            var objToAdd = new EditFeaturesParameter($"{JsonConvert.SerializeObject(new List<BaseFeatureWithGeometryObject<Polygon, ForesterBoundaryCase<int>>> { caseObj })}");


            var path = $"{_config.BaseUrl}{_config.Boundaries.Path}/addFeatures";
            var result = await PostQueryWithConversionAsync<CreateUpdateDeleteResponse<int>>(objToAdd, path, _config.NeedsToken, cancellationToken);

            //When using the rollback on errors the overall failure will happen here
            if (result.IsFailure)
            {
                _logger.LogError("Attempting to add application reference {CaseRef} to consultation public register failed with error {Error}", caseRef, result.Error);
                return Result.Failure<int>(result.Error);
            }

            if (result.Value.AddResults == null)
            {
                _logger.LogError("Attempting to add application reference {CaseRef} to consultation public register returned no results", caseRef);
                return Result.Failure<int>("No Results");
            }

            var errorResults = result.Value.AddResults.Where(r => r.ErrorDetails != null).Select(e => e.ErrorDetails);
            if (errorResults.Any())
            {
                _logger.LogError("Attempting to add application reference {CaseRef} to consultation public register returned errors: {Errors}", caseRef, string.Join(", ", errorResults.Select(r => r!.Details)));
                return Result.Failure<int>(string.Join(", ", errorResults.Select(r => r!.Details)));
            }

            var compartmentResult = await AddCompartmentsToCaseAsync(compartmentFeatures, cancellationToken);

            if (!compartmentResult.IsFailure)
            {
                return Result.Success<int>(result.Value.AddResults.First(r => r.WasSuccessful).ObjectId);
            }


            var deleteResult = await DeleteBoundariesAsync(result.Value.AddResults.First(r => r.WasSuccessful).ObjectId, cancellationToken);

            if (deleteResult.IsFailure)
            {
                _logger.LogError("Attempting to roll back boundaries for application with reference {CaseRef} from public register failed with error {Error}", caseRef, deleteResult.Error);
            }

            return Result.Failure<int>(deleteResult.IsFailure ? "Added Case Boundary, but failed to add compartments. Unable to rollback Boundary"
                : "Added Case Boundary, but failed to add compartments. Boundary has been rolled back");

        }

        /// <inheritdoc/>
        public async Task<Result> RemoveCaseFromConsultationRegisterAsync(int objectId, string caseReference, DateTime endDateOnPR, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Attempting to remove application reference {CaseRef} from consultation public register", caseReference);

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

            var boundaryCase = new List<ForesterBoundaryCase<int>>
            {
                new ForesterBoundaryCase<int>{
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

        /// <inheritdoc/>
        public async Task<Result> AddCaseToDecisionRegisterAsync(
            int objectId, 
            string caseReference, 
            string fellingLicenceOutcome,
            DateTime caseApprovalDateTime, 
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Attempting to publish application reference {CaseRef} to decision public register", caseReference);

            if (objectId == 0)
            {
                _logger.LogError("Object ID not correctly set for application having reference {CaseReference}", caseReference);
                return Result.Failure("Object ID not correctly set");
            }

            if (string.IsNullOrEmpty(caseReference))
            {
                _logger.LogError("No Case Reference given for application to publish to the decision public register");
                return Result.Failure("No Case Reference given");
            }

            var caseStatus = fellingLicenceOutcome.ToLower() switch
            {
                "approved" => "Approved",
                "refused" => "Refused",
                "referredtolocalauthority" => "Referred to LA",
                _ => null
            };

            if (caseStatus == null)
            {
                _logger.LogError("Felling application outcome of {FellingLicenceOutcome} on application having reference {CaseReference} is invalid for " +
                                 "sending to the Decision Public Register", fellingLicenceOutcome, caseReference);

                return Result.Failure($"Felling application outcome of {fellingLicenceOutcome} on application having reference {caseReference} is invalid for sending to the Decision Public Register");
            }

            var boundaryCase = new List<ForesterBoundaryCase<int>>
            {
                new()
                {
                    ObjectId = objectId,
                    CaseStatus = caseStatus,
                    CaseApprovalDate = caseApprovalDateTime,
                    OnThePR = "1"
                }
            };

            var featuresJson = JsonConvert.SerializeObject(boundaryCase.Single());
            var query = new EditFeaturesParameter($"[{{\"attributes\":{featuresJson}}}]");
            var path = $"{_config.BaseUrl}{_config.Boundaries.Path}/updateFeatures";

            var boundaryResult = await PostQueryWithConversionAsync<CreateUpdateDeleteResponse<int>>(query, path, _config.NeedsToken, cancellationToken);

            if (boundaryResult.IsFailure)
            {
                _logger.LogDebug("Attempting to publish application reference {CaseRef} to consultation public register", caseReference);
                return Result.Failure(boundaryResult.Error);
            }

            var compartmentUpdateResult = await UpdateCompartmentsByCaseRefAsync(caseReference, compartmentStatus: caseStatus, onPublicRegister: "1", cancellationToken: cancellationToken);

            if (compartmentUpdateResult.IsFailure)
            {
                _logger.LogError("Attempting to update compartments for application reference {CaseRef} on the Decision Public Register failed with error {Error}",
                    caseReference, compartmentUpdateResult.Error);
            }

            return compartmentUpdateResult.IsFailure ? Result.Failure($"Updated the Boundary, Updating Compartments returned '{compartmentUpdateResult.Error}'") : Result.Success();
        }

        /// <inheritdoc/>
        public async Task<Result> RemoveCaseFromDecisionRegisterAsync(int objectId, string caseReference, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Attempting to remove application reference {CaseRef} from decision public register", caseReference);

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

            var boundaryCase = new List<ForesterBoundaryCase<int>>
            {
                new ForesterBoundaryCase<int>{
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

        /// <summary>
        /// Update Compartments by the case reference ID.
        /// The Method Queries the Forester system to get the compartments against the case ref. it then applies the changes to the models
        /// any null values aren't rendered when the JSON is generated thus not updated/
        /// </summary>
        /// <param name="caseReference"></param>
        /// <param name="compartmentStatus"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="onPublicRegister"></param>
        /// <returns></returns>
        private async Task<Result> UpdateCompartmentsByCaseRefAsync(string caseReference, CancellationToken cancellationToken, string? compartmentStatus = null, string? onPublicRegister = null)
        {
            _logger.LogDebug("Attempting to update compartments for case reference {CaseReference} with status {CompartmentStatus} and onPublicRegister {OnPublicRegister}",
                caseReference, compartmentStatus, onPublicRegister);

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

            return errorResults.Any() ? Result.Failure<int>(string.Join(", ", errorResults.Select(r => r!.Details)))
                : Result.Success();
        }


        /// <summary>
        /// Adds the Compartments to the Forester System.
        /// </summary>
        /// <param name="compartmentFeatures">The compartment Features to add to the server</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<Result> AddCompartmentsToCaseAsync(List<BaseFeatureWithGeometryObject<Polygon, ForesterCompartment<int>>> compartmentFeatures, CancellationToken cancellationToken)
        {
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

            return errorResults.Any() ? Result.Failure(string.Join(", ", errorResults.Select(r => r!.Details)))
                : Result.Success();
        }

        /// <summary>
        /// If we fail to add compartments then we should rollback the case
        /// </summary>
        /// <param name="id">The ESRI ID  of the Record to delete</param>
        /// <param name="cancellationToken">The Cancellation Token</param>
        /// <returns>The Result of the task</returns>
        private async Task<Result> DeleteBoundariesAsync(int id, CancellationToken cancellationToken)
        {
            var body = new DeleteFeatureParameter(new List<int> { id });

            var path = $"{_config.BaseUrl}{_config.Boundaries.Path}/deleteFeatures";

            var result = await PostQueryWithConversionAsync<WasSuccessful>(body, path, _config.NeedsToken, cancellationToken);

            return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
        }
    }
}