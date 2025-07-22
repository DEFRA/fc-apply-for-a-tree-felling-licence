using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Infrastructure;
using Forestry.Flo.External.Web.Services.ExternalApi;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Controllers.Api;
   
/// <summary>
/// Controller class acting as the endpoint for the ESRI/Forester submission of an LIS Constraint PDF Report.
/// </summary>
[Route("api/lis/{applicationId}")]
[ApiController]
[RequiresValidApiKey]
public class LandInformationSearchApiController : ControllerBase
{
    private readonly ILogger<LandInformationSearchApiController> _logger;
    const string LisReportContentType = "application/pdf";
    private const string LisReportFileName = "LisReport.pdf";

    public LandInformationSearchApiController(
        ILogger<LandInformationSearchApiController> logger)
    {
        _logger = logger;
    }

    [HttpPut]
    [RequestSizeLimit(33554432)]//32MB 
    public async Task<IActionResult> StoreDocument(
        [FromRoute] string applicationId,
        [FromServices] AddDocumentFromExternalSystemUseCase useCase,
        CancellationToken cancellationToken)
    {
        var (_, isFailure, value, error) = await GetAsUseCaseRequestAsync(applicationId, Request, cancellationToken);
        
        if (isFailure)
            return error;
        
        return await useCase.AddLisConstraintReportAsync(
            value.applicationGuid, 
            value.fileBytes,
            LisReportFileName,
            LisReportContentType,
            DocumentPurpose.ExternalLisConstraintReport,
            cancellationToken);
    }

    private async Task<Result<(Guid applicationGuid, byte[] fileBytes), IActionResult>> GetAsUseCaseRequestAsync(
        string applicationId,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(applicationId, out var applicationGuid))
        {
            _logger.LogWarning("Supplied Felling Application Id [{applicationId}] in the request could not be parsed to the correct type."
                , applicationId);
            return BadRequest();
        }

        if (!string.Equals(request.ContentType, LisReportContentType,
                StringComparison.InvariantCultureIgnoreCase))
        {
            _logger.LogWarning("Supplied content type of [{actual}] in the request does not match the expected type of {expected}."
                , request.ContentType, LisReportContentType);
            return BadRequest();
        }
        
        var requestBody = await Request.GetRawBodyBytesAsync(cancellationToken);

        if (requestBody.Length == 0)
        {
            _logger.LogWarning("Supplied request body has zero length");
            return BadRequest();
        }

        return (applicationGuid, requestBody);
    }
}
