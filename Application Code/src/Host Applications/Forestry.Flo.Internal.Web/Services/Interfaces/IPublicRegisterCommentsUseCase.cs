using CSharpFunctionalExtensions;

namespace Forestry.Flo.Internal.Web.Services.Interfaces;

/// <summary>
/// Interface for use case handling retrieval of new comments from the Public Register for applications.
/// </summary>
public interface IPublicRegisterCommentsUseCase
{
    /// <summary>
    /// Gets new comments from the Public Register for all applications on the Consultation Public Register for yesterday.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A result containing a summary message or error.</returns>
    Task<Result<string>> GetNewCommentsFromPublicRegisterAsync(CancellationToken cancellationToken);
}
