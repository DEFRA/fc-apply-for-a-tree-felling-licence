using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis;
using Forestry.Flo.Services.Gis.Interfaces;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers;
using Forestry.Flo.Services.Gis.Models.Internal.Request;

namespace Forestry.Flo.External.Web.Services;
/// <summary>
/// Use case for validating the shape in the UI 
/// </summary>
public class ValidateShapeUseCase(IForesterServices iForesterServicesServices)
{
    /// <summary>
    /// Validates if the shape is in England
    /// </summary>
    /// <param name="requestObj">The request object to validate.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct containing a boolean flag with the result.</returns>
    public async Task<Result<Boolean>> IsInEnglandAsync(FlowShape<string> requestObj, CancellationToken cancellationToken)
    {
        var shape = ShapeHelper.ConvertShape(requestObj);
        if (shape.HasNoValue) 
        {
            return Result.Failure<Boolean>("Failed to read shape");
        }

        return await iForesterServicesServices.IsInEnglandAsync(shape.Value.ShapeDetails, cancellationToken);
    }

    /// <summary>
    /// Gets the Phytophthora Ramorum Risk Zones for the provided shape
    /// </summary>
    /// <param name="requestObj">The request object to check the zones for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> struct containing a list of zones for the input shape.</returns>
    public async Task<Result<List<PhytophthoraRamorumRiskZone>>> GetPhytophthoraRamorumRiskZonesAsync(FlowShape<string> requestObj, CancellationToken cancellationToken)
    {
        var shape = ShapeHelper.ConvertShape(requestObj);
        if (shape.HasNoValue) {
            return Result.Failure<List<PhytophthoraRamorumRiskZone>> ("Failed to read shape");
        }
        return await iForesterServicesServices.GetPhytophthoraRamorumRiskZonesAsync(shape.Value.ShapeDetails, cancellationToken);
    }
}