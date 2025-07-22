using System.Data.Common;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.EntityFrameworkCore.Storage;

namespace Forestry.Flo.Services.FellingLicenceApplications.Repositories;

public interface IFellingLicenceApplicationBaseRepository
{
    /// <summary>
    /// Unit of Work property to coordinate work with database  
    /// </summary>
    IUnitOfWork UnitOfWork { get; }


    /// <summary>
    /// Updates a given felling licence application.
    /// </summary>
    /// <param name="application">>A felling licence application to update.</param>
    void Update(FellingLicenceApplication application);


    /// <summary>
    /// Adds a status history entry for a felling licence application.
    /// </summary>
    /// <param name="userId">The Id of the user when updating the application</param>
    /// <param name="applicationId">A felling licence application guid</param>
    /// <param name="fellingLicenceStatus">The status of a felling licence application</param>
    /// <param name="cancellationToken">A cancellation token</param>
    Task AddStatusHistory(
        Guid userId,
        Guid applicationId, 
        FellingLicenceStatus fellingLicenceStatus,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sets the completion status for application steps of a <see cref="FellingLicenceApplication"/>
    /// </summary>
    /// <param name="applicationId">The id of the application.</param>
    /// <param name="applicationStepStatuses">A populated <see cref="ApplicationStepStatusRecord"/> containing optional completion statuses for each application step.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="UnitResult"/> indicating whether the application step statuses have been successfully updated, or a <see cref="UserDbErrorReason"/> if unsuccessful.</returns>
    Task<UnitResult<UserDbErrorReason>> UpdateApplicationStepStatusAsync(
        Guid applicationId,
        ApplicationStepStatusRecord applicationStepStatuses,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the case notes for an application that optionally match a given list of case note types.
    /// </summary>
    /// <param name="applicationId">The application id to retrieve case notes for.</param>
    /// <param name="caseNoteTypes">An optional list of case note types to filter the list by.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    Task<IList<CaseNote>> GetCaseNotesAsync(
        Guid applicationId, 
        CaseNoteType[] caseNoteTypes, 
        CancellationToken cancellationToken);


    /// <summary>
    /// Attempts to retrieve a specific document from an application by application Id.
    /// </summary>
    /// <remarks>.</remarks>
    /// <param name="applicationId">The Id of the FLA to retrieve.</param>
    /// <param name="documentIdentifier">The Id of the document.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Document"/> entity, if it was located with the given values, otherwise no value.</returns>
    Task<Maybe<Document>> GetDocumentByIdAsync(
        Guid applicationId,
        Guid documentIdentifier,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes the given document entity from the repository entirely.
    /// </summary>
    /// <param name="document">The <see cref="Document"/> entity to hard delete from the repository.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    Task<UnitResult<UserDbErrorReason>> DeleteDocumentAsync(Document document, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a document to an application.
    /// </summary>
    /// <param name="document">The <see cref="Document"/> entity to add to the repository.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    Task<UnitResult<UserDbErrorReason>> AddDocumentAsync(Document document, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if an application exists with the given id.
    /// </summary>
    /// <param name="applicationId">The id of the felling licence application to check.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns></returns>
    Task<bool> CheckApplicationExists(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a given user is allowed to access the application with the given Id.
    /// </summary>
    /// <param name="applicationId">The id of the application to check.</param>
    /// <param name="userAccess">A representation of the users access to data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the user can access the application, otherwise false.</returns>
    Task<Result<bool>> CheckUserCanAccessApplicationAsync(Guid applicationId, UserAccessModel userAccess, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the application reference for the application with the given id.
    /// </summary>
    /// <param name="applicationId">The application id.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The application reference, or <see cref="Result.Failure"/> if it could not be retrieved.</returns>
    Task<Result<string>> GetApplicationReferenceAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes any existing admin officer review entry for the application with the given id.
    /// </summary>
    /// <param name="applicationId">The id of the application to delete the admin officer review for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    Task<UnitResult<UserDbErrorReason>> DeleteAdminOfficerReviewForApplicationAsync(Guid applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// Begins a transaction for the current context.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Task{IDbContextTransaction}"/> representing the transaction.</returns>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken);

    DbConnection GetDbConnection();
}