using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.Layers;
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
    /// Retrieves a list of ancient woodland records based on the provided geometric shape.
    /// </summary>
    /// <param name="shape">
    /// The geometric shape used to query ancient woodland data.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation. The task result contains a <see cref="Result{T}"/> 
    /// with a list of <see cref="AncientWoodland"/> objects if the operation is successful, or an error message if it fails.
    /// </returns>
    Task<Result<List<AncientWoodland>>> GetAncientWoodlandAsync(BaseShape shape,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a list of revised ancient woodland records based on the provided geographical shape.
    /// </summary>
    /// <param name="shape">
    /// The geographical shape used to query the revised ancient woodland data.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A result containing a list of <see cref="AncientWoodland"/> objects if the query is successful; otherwise, a failure result with an error message.
    /// </returns>
    /// <remarks>
    /// This method queries a specific layer ("Ancient_Woodlands_Revised") for ancient woodland data.
    /// If the layer details are unavailable or no results are found, the method returns a failure result.
    /// </remarks>
    Task<Result<List<AncientWoodland>>> GetAncientWoodlandsRevisedAsync(BaseShape shape,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the Phytophthora Ramorum Risk Zones the shape intersects with.
    /// </summary>
    /// <param name="shape">The shape that we're going to check</param>
    /// <param name="cancellationToken">The cancellation Token</param>
    /// <returns>A list of zones the shape touches</returns>
    Task<Result<List<PhytophthoraRamorumRiskZone>>> GetPhytophthoraRamorumRiskZonesAsync(BaseShape shape,
        CancellationToken cancellationToken);

    /// <summary>
    /// Publishes Forestry Land Application (FLA) data to an external system.
    /// </summary>
    /// <param name="applicationRef">The unique reference identifier for the application.</param>
    /// <param name="applicationStatus">The current status of the application.</param>
    /// <param name="hasConditions">Indicates whether the application has conditions associated with it.</param>
    /// <param name="expiryCategory">The category of expiry for the application.</param>
    /// <param name="compartments">A list of compartment details, including their geometry and metadata.</param>
    /// <param name="exDate">The optional expiry date for the application.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the operation.</returns>
    Task<Result> Publish_FLAToExternalAsync(
        string applicationRef,
        string applicationStatus,
        bool hasConditions,
        string expiryCategory,
        List<InternalCompartmentDetails<Polygon>> compartments,
        DateTime? exDate,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Publishes Forest Land Application (FLA) details to the internal system.
    /// </summary>
    /// <param name="propertyName">The name of the property associated with the application.</param>
    /// <param name="applicationRef">The unique reference identifier for the application.</param>
    /// <param name="gridReference">The grid reference of the property.</param>
    /// <param name="nearestTown">The nearest town to the property.</param>
    /// <param name="adminHubAreaName">The name of the administrative hub area.</param>
    /// <param name="consultationPublicRegisterStartDate">The start date for the consultation public register.</param>
    /// <param name="consultationPublicRegisterPeriod">The duration of the consultation public register period in days.</param>
    /// <param name="consultationPublicRegisterEndDate">The end date for the consultation public register.</param>
    /// <param name="decisionPublicRegisterStartDate">The start date for the decision public register.</param>
    /// <param name="decisionPublicRegisterPeriod">The duration of the decision public register period in days.</param>
    /// <param name="decisionPublicRegisterEndDateTime">The end date and time for the decision public register.</param>
    /// <param name="compartmentLabel">The label for the compartment associated with the application.</param>
    /// <param name="confirmedTotalAreaHa">The confirmed total area of the property in hectares.</param>
    /// <param name="fsAreaName">The name of the Forestry Service area.</param>
    /// <param name="currentApplicationStatus">The current status of the application.</param>
    /// <param name="applicant">The name of the applicant.</param>
    /// <param name="hasConditions">Indicates whether the application has conditions.</param>
    /// <param name="submittedDate">The date the application was submitted.</param>
    /// <param name="decisionDate">The date a decision was made on the application.</param>
    /// <param name="withdrawalDate">The date the application was withdrawn, if applicable.</param>
    /// <param name="adminOfficer">The name of the administrative officer handling the application.</param>
    /// <param name="woodLandOfficer">The name of the woodland officer handling the application.</param>
    /// <param name="approvingOfficer">The name of the approving officer for the application.</param>
    /// <param name="expiredDate">The expiration date of the application.</param>
    /// <param name="expiryCategory">The category of the expiry.</param>
    /// <param name="compartments">A list of compartment details associated with the application.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Result"/> indicating the success or failure of the operation.</returns>
    Task<Result> Publish_FLAToInternalAsync(
        string? propertyName,
        string applicationRef,
        string? gridReference,
        string? nearestTown,
        string? adminHubAreaName,
        DateTime? consultationPublicRegisterStartDate,
        int? consultationPublicRegisterPeriod,
        DateTime? consultationPublicRegisterEndDate,
        DateTime? decisionPublicRegisterStartDate,
        int? decisionPublicRegisterPeriod,
        DateTime? decisionPublicRegisterEndDateTime,
        string? compartmentLabel,
        float? confirmedTotalAreaHa,
        string? fsAreaName,
        string? currentApplicationStatus,
        string? applicant,
        bool hasConditions,
        DateTime? submittedDate,
        DateTime? decisionDate,
        DateTime? withdrawalDate,
        string? adminOfficer,
        string? woodLandOfficer,
        string? approvingOfficer,
        DateTime? expiredDate,
        string? expiryCategory,
        List<InternalCompartmentDetails<Polygon>> compartments,
        CancellationToken cancellationToken);

}

