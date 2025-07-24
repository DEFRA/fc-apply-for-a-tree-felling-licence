namespace Forestry.Flo.Services.FellingLicenceApplications.Repositories;

public interface IFellingLicenceApplicationReferenceRepository
{
    /// <summary>
    /// Gets the next felling licence application reference number value from the repository.
    /// </summary>
    /// <param name="year">The year value to look up the next reference for.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The next sequence value for an application reference.</returns>
    Task<long> GetNextApplicationReferenceIdValueAsync(int year, CancellationToken cancellationToken);
}