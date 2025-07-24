using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.ConditionsBuilder.Entities;

namespace Forestry.Flo.Services.ConditionsBuilder.Repositories;

public interface IConditionsBuilderRepository
{
    /// <summary>
    /// Unit of Work property to coordinate work with database  
    /// </summary>
    IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// Retrieves any existing felling licence application conditions for a given
    /// application id.
    /// </summary>
    /// <param name="applicationId">The id of the application to retrieve conditions for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A list of any existing conditions for the application.</returns>
    Task<IList<FellingLicenceCondition>> GetConditionsForApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Removes any conditions from the repository for a given application id.
    /// </summary>
    /// <param name="applicationId">The id of the application to remove existing conditions for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <remarks>The caller must subsequently call UnitOfWork.SaveEntitiesAsync to complete the transaction.</remarks>
    Task ClearConditionsForApplicationAsync(
        Guid applicationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stores a collection of new felling licence application conditions to the repository.
    /// </summary>
    /// <param name="conditions">A collection of new conditions.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <remarks>The caller must subsequently call UnitOfWork.SaveEntitiesAsync to complete the transaction.</remarks>
    Task SaveConditionsForApplicationAsync(
        IList<FellingLicenceCondition> conditions,
        CancellationToken cancellationToken);
}