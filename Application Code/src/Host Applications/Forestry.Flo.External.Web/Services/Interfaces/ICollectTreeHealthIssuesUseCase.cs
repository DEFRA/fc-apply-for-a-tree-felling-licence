using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication.TreeHealth;

namespace Forestry.Flo.External.Web.Services.Interfaces;

/// <summary>
/// Use case class for collection of any tree health issues involved in an application
/// from the applicant.
/// </summary>
public interface ICollectTreeHealthIssuesUseCase
{
    /// <summary>
    /// Gets the view model for tree health issues in a felling licence application.
    /// </summary>
    /// <param name="applicationId">The id of the application being viewed.</param>
    /// <param name="user">The user viewing the application.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A populated <see cref="TreeHealthIssuesViewModel"/> to show in the view.</returns>
    Task<Result<TreeHealthIssuesViewModel>> GetTreeHealthIssuesViewModelAsync(
        Guid applicationId,
        ExternalApplicant user,
        CancellationToken cancellationToken);

    /// <summary>
    /// Submits the tree health issues for a felling licence application.
    /// </summary>
    /// <param name="applicationId">The Id of the application being updated.</param>
    /// <param name="user">The user updating the application.</param>
    /// <param name="model">A model of the tree health issues data.</param>
    /// <param name="treeHealthFiles">A collection of attachments for the tree health issues.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="SubmitTreeHealthIssuesError"/> if there is any failure.</returns>
    Task<UnitResult<SubmitTreeHealthIssuesError>> SubmitTreeHealthIssuesAsync(
        Guid applicationId,
        ExternalApplicant user,
        TreeHealthIssuesViewModel model,
        FormFileCollection treeHealthFiles,
        CancellationToken cancellationToken);
}