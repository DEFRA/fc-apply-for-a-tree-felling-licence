using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers;
using Forestry.Flo.Services.Gis.Models.Esri.Responses;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;

namespace Forestry.Flo.Services.Gis.Interfaces;

/// <summary>
/// The Access class for handling calls to the Arch GIS Online Services 
/// </summary>
public interface IForesterServices
{
    /// <summary>
    /// Gets the "woodland" officer for the point given
    /// </summary>
    /// <param name="centralCasePoint">The Point that we're going to check</param>
    /// <param name="cancellationToken">The cancellation Token</param>
    /// <returns>The area name, officer name, Officer Manager and ID of the given point. NOTE: Its Possible that the name/id returned is null</returns>
    Task<Result<WoodlandOfficer>> GetWoodlandOfficerAsync(Point centralCasePoint,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the "woodland" officer for the compartments given
    /// </summary>
    /// <param name="compartments">The Compartments to use</param>
    /// <param name="cancellationToken">The cancellation Token</param>
    /// <returns></returns>
    Task<Result<WoodlandOfficer>> GetWoodlandOfficerAsync(List<string> compartments,
       CancellationToken cancellationToken);

    /// <summary>
    /// Gets the first admin boundary for the shape given
    /// </summary>
    /// <param name="shape">The shape that we're going to check</param>
    /// <param name="cancellationToken">The cancellation Token</param>
    /// <returns>The Admin area name and ID of the given point. NOTE: Its Possible that the name/id returned is null</returns>
    Task<Result<AdminBoundary>> GetAdminBoundaryIdAsync(BaseShape shape, CancellationToken cancellationToken);

    /// <summary>
    /// If the shape is in England
    /// </summary>
    /// <param name="shape">The shape that we're going to check</param>
    /// <param name="cancellationToken">The cancellation Token</param>
    /// <returns></returns>
    Task<Result<Boolean>> IsInEnglandAsync(BaseShape shape,
        CancellationToken cancellationToken);

    /// <summary>
    /// Generates an image based on the compartment passed in
    /// </summary>
    /// <param name="compartmentDetails">The Shape to draw on the map</param>
    /// <param name="cancellationToken">The cancellation Token</param>
    /// <param name="delay">Optional setting for the delay to leave between calls</param>
    /// <param name="generationType">Option setting for helping to set the title</param>
    /// <param name="title">Option setting for helping to set the title</param>
    /// <returns>A stream of the image</returns>
    Task<Result<Stream>> GenerateImage_SingleCompartmentAsync(InternalCompartmentDetails<BaseShape> compartmentDetails, CancellationToken cancellationToken, int delay = 30000, MapGeneration generationType = MapGeneration.Other, string title = "");


    /// <summary>
    /// Generates an image based on the compartment passed in
    /// </summary>
    /// <param name="compartments">The Compartments that belong to the case</param>
    /// <param name="cancellationToken">The cancellation Token</param>
    /// <param name="delay">Optional setting for the delay to leave between calls</param>
    /// <param name="generationType">Option setting for helping to set the title</param>
    /// <param name="title">Option setting for helping to set the title</param>
    /// <returns>A stream of the image</returns>
    Task<Result<Stream>> GenerateImage_MultipleCompartmentsAsync(List<InternalCompartmentDetails<BaseShape>> compartments,
        CancellationToken cancellationToken, int delay = 30000, MapGeneration generationType = MapGeneration.Other, string title = "");

    /// <summary>
    /// Gets the Local Authority (Local Council) for the point given
    /// </summary>
    /// <param name="centralCasePoint">The Point that we're going to check</param>
    /// <param name="cancellationToken">The cancellation Token</param>
    /// <returns>The Admin area name and ID of the given point. NOTE: Its Possible that the name/id returned is null</returns>
    Task<Result<LocalAuthority>> GetLocalAuthorityAsync(Point centralCasePoint,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the Phytophthora Ramorum Risk Zones the shape intersects with.
    /// </summary>
    /// <param name="shape">The shape that we're going to check</param>
    /// <param name="cancellationToken">The cancellation Token</param>
    /// <returns>A list of zones the shape touches</returns>
    Task<Result<List<PhytophthoraRamorumRiskZone>>> GetPhytophthoraRamorumRiskZonesAsync(BaseShape shape,
        CancellationToken cancellationToken);

}

