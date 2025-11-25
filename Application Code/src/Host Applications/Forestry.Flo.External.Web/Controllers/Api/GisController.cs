using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Services;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.Gis.Models.Internal.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Forestry.Flo.External.Web.Controllers.Api
{
    [ApiController, Authorize, Route("api/[controller]")]
    public class GisController : ControllerBase
    {
        private readonly ILogger<GisController> _logger;
        private readonly CreateFellingLicenceApplicationUseCase _fellingUseCase;

        public GisController(ILogger<GisController> logger, CreateFellingLicenceApplicationUseCase fellingUseCase)
        {
            _logger = logger;
            _fellingUseCase = fellingUseCase;
        }

        [AllowAnonymous, HttpGet, Route("heartBeat")]
        public ActionResult HeartBeat()
        {
            return Ok(new
            {
                beat = DateTime.Now
            });
        }

        [HttpGet, Route("UploadSettings")]
        public ActionResult GetUploadSettings([FromServices] UploadShapeFileUseCase mapping)
        {
            var supportedFileTypes = mapping.GetSupportedFileTypes();
            if (supportedFileTypes.HasValue)
            {
                return Ok(new { supportedFileTypes= supportedFileTypes.Value, maxSize = (new UserFileUploadOptions()).ServerMaxUploadSizeBytes});
            }

            return BadRequest("Unable to load file types");
        }

        [HttpPost, Route("GetShapes")]
        public async Task<ActionResult> GetShapesFromFileAsync([FromServices] UploadShapeFileUseCase mapping, [FromForm] string name,
            [FromForm] string ext, [FromForm] bool generalize,
            [FromForm] int offset, [FromForm] bool reduce, [FromForm] int round, [FromForm] IFormFile file, CancellationToken cancellationToken)
        {

            Guard.Against.Null(file, nameof(file));
            try
            {
                var resx = await mapping.GetShapesFromFileAsync(name, ext, generalize, offset, reduce, round,
                    file, cancellationToken);

                if (resx.IsSuccess)
                {
                    return Ok(resx.Value);
                }
                return BadRequest(resx.Error);
            }
            catch (ArgumentNullException ane)
            {
                //In theory this shouldn't happened, but will aim bug tracking
                _logger.LogError(ane, "Value Not set");
                return BadRequest("File not correctly set");
            }
            catch (Exception ex)
            {
                //In theory this shouldn't happened, but will aim bug tracking
                _logger.LogError(ex, "General Error");
                return BadRequest("Failure to process file");
            }
        }

        [HttpPost, Route("GetShapesFromString")]
        public async Task<ActionResult> GetShapesFromStringAsync([FromServices] UploadShapeFileUseCase mapping, [FromForm] string name,
           [FromForm] string ext, [FromForm] bool generalize,
           [FromForm] int offset, [FromForm] bool reduce, [FromForm] int round, [FromForm] string valueString, CancellationToken cancellationToken)
        {

            Guard.Against.NullOrEmpty(valueString);
            try
            {
                var resx = await mapping.GetShapesFromStringAsync(name, ext, generalize, offset, reduce, round,
                     valueString, cancellationToken);

                if (resx.IsSuccess)
                {
                    return Ok(resx.Value);
                }
                return BadRequest(resx.Error);
            }
            catch (ArgumentNullException ane)
            {
                //In theory this shouldn't happened, but will aim bug tracking
                _logger.LogError(ane, "Value Not set");
                return BadRequest("File not correctly set");
            }
            catch (Exception ex)
            {
                //In theory this shouldn't happened, but will aim bug tracking
                _logger.LogError(ex, "General Error");
                return BadRequest("Failure to process file");
            }
        }

        [HttpGet, Route("GetPropertyDetails")]
        public async Task<ActionResult> GetPropertyDetailsForBulkImport([FromQuery] Guid propertyGuid,
            [FromServices] ManagePropertyProfileUseCase propertyUseCase,
            [FromServices] ManageGeographicCompartmentUseCase compartmentUseCase, CancellationToken cancellationToken)
        {
            var user = new ExternalApplicant(User);

            var result = await compartmentUseCase.VerifyUserPropertyProfileAsync(user, propertyGuid, cancellationToken);
            if (result.IsFailure)
            {
                return Unauthorized();
            }


            var allPropertyCompartments = await propertyUseCase.RetrievePropertyProfileCompartments(propertyGuid,
                user, cancellationToken);

            return Ok(new
            {
                nearestTown = allPropertyCompartments.Value.NearestTown,
                allPropertyCompartments = allPropertyCompartments.Value.Compartments
            });
        }

        [HttpPost, Route("FellingAndRestockingSelectedCompartments")]
        public async Task<ActionResult> FellingAndRestockingSelectedCompartmentsAsync(Guid? applicationId, CancellationToken cancellationToken)
        {
            var user = new ExternalApplicant(User);
            if (applicationId == null)
            {
                return BadRequest("Unable to validate model");
            }
            var resx = await _fellingUseCase.GetSelectedCompartmentsAsync((Guid)applicationId, user, cancellationToken);
            return resx.HasValue
                ? Ok(JsonConvert.SerializeObject(resx.Value))
                : BadRequest();
        }

        [HttpPost, Route("Import")]
        public async Task<ActionResult> BulkImportAsync([FromBody] BulkImportCompartment requestObj,
            [FromServices] ManageGeographicCompartmentUseCase compartmentUseCase, CancellationToken cancellationToken)
        {
            var user = new ExternalApplicant(User);

            var userCheck = await compartmentUseCase.VerifyUserPropertyProfileAsync(user, requestObj.PropertyProfile, cancellationToken);
            if (userCheck.IsFailure)
            {
                return Unauthorized();
            }
            var successfulShapes = new List<int>();
            var failedShapes = new Dictionary<int, string>();
            foreach (var item in requestObj.Compartments)
            {
                var (_, isFailure, id, error) = await compartmentUseCase.CreateCompartmentAsync(item, requestObj.PropertyProfile, user, cancellationToken);
                if (isFailure)
                {
                    if(error.ErrorType == ErrorTypes.Conflict && error.FieldName is not null &&
                           error.FieldName == nameof(item.CompartmentNumber))
                    {
                        failedShapes.Add(item.ShapeID, $"{item.CompartmentNumber} is already in use. Please rename add try again");
                    }
                    else
                    {
                        failedShapes.Add(item.ShapeID, $"Unable to save {item.CompartmentNumber}");
                    }
                }
                else
                {
                    successfulShapes.Add(item.ShapeID);
                }
            }

            return Ok(new { success = successfulShapes, failures = failedShapes.Select(n => new { shapeID = n.Key, value = n.Value }) });
        }

        [HttpPost, Route("IsInEngland")]
        public async Task<ActionResult> IsInEngland([FromServices] ValidateShapeUseCase mapping, [FromBody] FlowShape<string> requestObj, CancellationToken cancellationToken)
        {
            var resx = await mapping.IsInEnglandAsync(requestObj, cancellationToken);
            return Ok(new {isSuccess = resx.IsSuccess, result =  resx.Value });
        }

        [HttpPost, Route("GetZones")]
        public async Task<ActionResult> GetZones([FromServices] ValidateShapeUseCase mapping, [FromBody] FlowShape<string> requestObj, CancellationToken cancellationToken)
        {
            var resx = await mapping.GetPhytophthoraRamorumRiskZonesAsync(requestObj, cancellationToken);
            return Ok(new { isSuccess = resx.IsSuccess, result = resx.IsSuccess ? resx.Value.ToArray() : [] });
        }
    }
}
