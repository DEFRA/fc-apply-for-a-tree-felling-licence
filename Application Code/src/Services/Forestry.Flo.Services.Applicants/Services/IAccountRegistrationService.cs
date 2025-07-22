using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Models;

namespace Forestry.Flo.Services.Applicants.Services;

/// <summary>
/// Contract for a service that implements tasks to register a new user account
/// </summary>
public interface IAccountRegistrationService
{
    /// <summary>
    /// Adds a new <see cref="UserAccount"/> entity to the system
    /// </summary>
    /// <param name="request">A populated <see cref="AddExternalUserRequest"/> model with details of
    /// /// the agent authority to be added.</param>;
    /// <param name="cancellationToken"></param>
    /// <returns>A populated <see cref="AddExternalUserResponse"/> model, or a failure result.</returns>
    Task<Result<AddExternalUserResponse>> CreateUserAccountAsync(
        AddExternalUserRequest request, 
        CancellationToken cancellationToken);
}

