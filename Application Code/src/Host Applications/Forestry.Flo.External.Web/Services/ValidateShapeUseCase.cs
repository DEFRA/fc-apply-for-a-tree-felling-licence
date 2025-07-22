using System.Collections.Generic;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers;
using Forestry.Flo.Services.Gis.Models.Internal.Request;

namespace Forestry.Flo.External.Web.Services;
/// <summary>
/// Use case for validating the shape in the UI 
/// </summary>
public class ValidateShapeUseCase
{
    private readonly IForesterServices _iForesterServicesServices;
    private readonly ILogger<ValidateShapeUseCase> _logger;

    public ValidateShapeUseCase(IForesterServices iForesterServicesServices, ILogger<ValidateShapeUseCase> logger)
    {
        _iForesterServicesServices = iForesterServicesServices;
        _logger = logger;
    }

    public async Task<Result<Boolean>> IsInEnglandAsync(FlowShape<string> requestObj, CancellationToken cancellationToken)
    {
        var shape = ShapeHelper.ConvertShape(requestObj);
        if (shape.HasNoValue) {
            return Result.Failure<Boolean>("Failed to read shape");
        }

        return await _iForesterServicesServices.IsInEnglandAsync(shape.Value.ShapeDetails, cancellationToken);
    }

    public async Task<Result<List<PhytophthoraRamorumRiskZone>>> GetPhytophthoraRamorumRiskZonesAsync(FlowShape<string> requestObj, CancellationToken cancellationToken)
    {
        var shape = ShapeHelper.ConvertShape(requestObj);
        if (shape.HasNoValue) {
            return Result.Failure<List<PhytophthoraRamorumRiskZone>> ("Failed to read shape");
        }
        return await _iForesterServicesServices.GetPhytophthoraRamorumRiskZonesAsync(shape.Value.ShapeDetails, cancellationToken);
    }
}