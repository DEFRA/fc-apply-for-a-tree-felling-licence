using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.PropertyProfiles;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Services;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Forestry.Flo.External.Web.Services;

public class WoodlandOwnerHomePageUseCase
{
    private readonly IRetrieveUserAccountsService _retrieveUserAccountsService;
    private readonly IGetPropertyProfiles _getPropertyProfilesService;
    private readonly ILogger<WoodlandOwnerHomePageUseCase> _logger;

    public WoodlandOwnerHomePageUseCase(
        IGetPropertyProfiles getPropertyProfilesService,
        IRetrieveUserAccountsService retrieveUserAccountsService,
        ILogger<WoodlandOwnerHomePageUseCase> logger)
    {
        _getPropertyProfilesService = Guard.Against.Null(getPropertyProfilesService);
        _retrieveUserAccountsService = Guard.Against.Null(retrieveUserAccountsService);
        _logger = logger;
    }

    public async Task<Result<IEnumerable<PropertyProfile>>> RetrievePropertyProfilesForWoodlandOwnerAsync(
        Guid woodlandOwnerId,
        ExternalApplicant user, 
        CancellationToken cancellationToken)
    {
        var userAccess = await _retrieveUserAccountsService
            .RetrieveUserAccessAsync(user.UserAccountId!.Value, cancellationToken)
            .ConfigureAwait(false);

        var getPropertiesResult = await _getPropertyProfilesService.ListAsync(
            new ListPropertyProfilesQuery(woodlandOwnerId), 
            userAccess.Value, 
            cancellationToken);

        if (getPropertiesResult.IsFailure)
        {
            _logger.LogWarning("Unable to retrieve properties for user having id of {userId}, " +
                               "for the Woodland Owner Id supplied of {woodlandOwnerId}", user.UserAccountId, woodlandOwnerId);

            return Result.Failure<IEnumerable<PropertyProfile>>($"Unable to list properties for user with userId of {user.UserAccountId} and woodland owner id supplied of {woodlandOwnerId}");
        }

        return Result.Success(getPropertiesResult.Value);
    }
}