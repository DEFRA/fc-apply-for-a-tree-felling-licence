using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Gis.Models.Esri.Responses.PublicRegister;
using Forestry.Flo.Services.Gis.Models.Internal;
using Forestry.Flo.Services.Gis.Models.Internal.MapObjects;
using Forestry.Flo.Services.Gis.Models.Internal.Request;

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
        /// <param name="dataModel">A populated <see cref="AddToPublicRegisterModel"/> containing the case details.</param>
        /// <param name="cancellationToken">The Cancellation Token</param>
        /// <returns>The ID of the Added Boundary</returns>
        Task<Result<int>> AddCaseToConsultationRegisterAsync(AddToPublicRegisterModel dataModel, CancellationToken cancellationToken);

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
        /// <param name="dataModel">A populated <see cref="AddToDecisionPublicRegisterModel"/> containing the case details.</param>
        /// <param name="cancellationToken">The Cancellation Token</param>
        /// <returns>The ID of the Added Boundary</returns>
        Task<Result<int>> AddCaseToDecisionRegisterAsync(
            AddToDecisionPublicRegisterModel dataModel, 
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
