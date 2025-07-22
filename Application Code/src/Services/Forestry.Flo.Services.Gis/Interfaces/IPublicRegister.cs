using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.PublicRegister;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;

namespace Forestry.Flo.Services.Gis.Interfaces
{
    /// <summary>
    /// Providing all calls to the land register system
    /// </summary>
    public interface IPublicRegister
    {
        /// <summary>
        /// Gets the Comments for a case reference.
        /// </summary>
        /// <param name="caseReference">The Case Reference ID</param>
        /// <param name="cancellationToken"></param>
        /// <returns>All the comments for the case reference</returns>
        Task<Result<List<EsriCaseComments>>> GetCaseCommentsByCaseReferenceAsync(string caseReference, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the Comments for a case reference that where created on a date.
        /// </summary>
        /// <param name="caseReference">The Case Reference ID</param>
        /// <param name="date">The date only value of the visit date</param>
        /// <param name="cancellationToken"></param>
        /// <returns>All the comments for the visit</returns>
        Task<Result<List<EsriCaseComments>>> GetCaseCommentsByCaseReferenceAndDateAsync(string caseReference, DateOnly date, CancellationToken cancellationToken);

        /// <summary>
        /// Adds a Case boundary to the Forester system
        /// </summary>
        /// <param name="caseRef">The case reference</param>
        /// <param name="propertyName">The Property Name</param>
        /// <param name="caseType">The Case type</param>
        /// <param name="gridRef"></param>
        /// <param name="nearestTown">The Nearest town of the boundary</param>
        /// <param name="localAdminArea">The Local Authority</param>
        /// <param name="adminRegion">The Admin region for the FC</param>
        /// <param name="publicRegisterStart">Public Register Start Date</param>
        /// <param name="period">The Length of time on the consultation Register</param>
        /// <param name="broadLeafArea">The Broad Leaf Area (ha)</param>
        /// <param name="coniferousArea">The Coniferous Area (ha)</param>
        /// <param name="openGroundArea">The Open Ground Area (ha)</param>
        /// <param name="totalArea">The Total Area (HA)</param>
        /// <param name="compartments">The Compartments to add to the case</param>
        /// <param name="cancellationToken">The Cancellation Token</param>
        /// <returns>The ID of the Added Boundary</returns>
        Task<Result<int>> AddCaseToConsultationRegisterAsync(string caseRef, string propertyName, string caseType, string gridRef, string nearestTown, string localAdminArea, string adminRegion,
                     DateTime publicRegisterStart, int period, double? broadLeafArea, double? coniferousArea, double? openGroundArea, double? totalArea, List<InternalCompartmentDetails<Polygon>> compartments, CancellationToken cancellationToken);

        /// <summary>
        /// Removes a case from the Consultation Register
        /// </summary>
        /// <param name="objectId">The Esri ID for the case. Created in the AddCaseToConsultationRegisterAsync method</param>
        /// <param name="caseReference">The case reference. This MUST match the case ref given when adding the case to the Consultation Register. As this is used to update the compartments</param>
        /// <param name="endDateOnPR">The date the item is to be removed</param>
        /// <param name="cancellationToken">The Cancellation Token</param>
        /// <returns>A result of the actions</returns>
        Task<Result> RemoveCaseFromConsultationRegisterAsync(int objectId, string caseReference, DateTime endDateOnPR, CancellationToken cancellationToken);

        /// <summary>
        /// Add case to the Decision Register
        /// </summary>
        /// <param name="objectId">The Esri ID for the case. Created in the AddCaseToConsultationRegisterAsync method</param>
        /// <param name="caseReference">The case reference. This MUST match the case ref given when adding the case to the Consultation Register. As this is used to update the compartments</param>
        /// <param name="fellingLicenceOutcome">The felling licence status outcome</param>
        /// <param name="caseApprovalDateTime">The date to add the case to the decision register</param>
        /// <param name="cancellationToken">The Cancellation Token</param>
        /// <returns>A result of the actions</returns>
        Task<Result> AddCaseToDecisionRegisterAsync(
            int objectId, 
            string caseReference, 
            string fellingLicenceOutcome, 
            DateTime caseApprovalDateTime, 
            CancellationToken cancellationToken);

        /// <summary>
        /// Removes a case from the Decision Register
        /// </summary>
        /// <param name="objectId">The Esri ID for the case. Created in the AddCaseToConsultationRegisterAsync method</param>
        /// <param name="caseReference">The case reference. This MUST match the case ref given when adding the case to the Consultation Register. As this is used to update the compartments</param>
        /// <param name="cancellationToken"></param>
        /// <returns>A result of the actions</returns>
        Task<Result> RemoveCaseFromDecisionRegisterAsync(int objectId, string caseReference, CancellationToken cancellationToken);
    }
}
