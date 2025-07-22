using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.External.Web.Services;

/// <summary>
/// Handles an applicant user selecting to run the Constraint Check in the external web application.
/// </summary>
public class RunApplicantConstraintCheckUseCase
{
    private readonly ConstraintCheckerService _constraintCheckerService;
    private readonly ILogger<RunApplicantConstraintCheckUseCase> _logger;

    public RunApplicantConstraintCheckUseCase(
        ConstraintCheckerService constraintCheckerService,
        ILogger<RunApplicantConstraintCheckUseCase> logger)
    {
        _constraintCheckerService = Guard.Against.Null(constraintCheckerService);
        _logger = logger;
    }

    public async Task<Result<RedirectResult>> ExecuteConstraintsCheckAsync(
        ExternalApplicant user,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Constraint Check has been requested for application having Id of [{id}].", applicationId);

        var request = ConstraintCheckRequest.Create(user.Principal, applicationId);

        var result = await _constraintCheckerService.ExecuteAsync(request, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogDebug(
                "Successfully ran all Constraint Check pre-requisites for application having id of [{id}], received deep-link Url to use of [{url}]."
                , applicationId, result.Value.ToString());
                             
            return Result.Success(new RedirectResult(result.Value.ToString()));
        }
        
        _logger.LogWarning("Unable to execute Constraint Check for application having id of [{id}], received error [{error}]."
            , applicationId, result.Error);

        return Result.Failure<RedirectResult>("failure"); //not logged, failure results in redirect to error page.
    }

}