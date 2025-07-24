using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using Forestry.Flo.External.Web.Models.UserAccount;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common.Extensions;

namespace Forestry.Flo.External.Web.Services;

public class ListWoodlandOwnerUsersUseCase
{
    private readonly IRetrieveUserAccountsService _retrieveUserAccountsService;
    private readonly ILogger<ListWoodlandOwnerUsersUseCase> _logger;


    public ListWoodlandOwnerUsersUseCase(
        IRetrieveUserAccountsService retrieveUserAccountsService,
        ILogger<ListWoodlandOwnerUsersUseCase> logger)
    {
        _retrieveUserAccountsService = Guard.Against.Null(retrieveUserAccountsService);
        _logger = logger;
    }

    public async Task<Result<ListUsersLinkedWoodlandOwnerModel>> RetrieveListOfWoodlandOwnerUsersAsync(
        ExternalApplicant user,
        Guid woodlandOwnerId,
        CancellationToken cancellationToken)
    {
        var resultUsers = await _retrieveUserAccountsService.RetrieveUserAccountsForWoodlandOwnerAsync(woodlandOwnerId, cancellationToken);
        if (resultUsers.IsFailure)
        {
            _logger.LogError("Could not retrieve the users linked to the Woodland Owner: {UserWoodlandOwnerId}", user.WoodlandOwnerId);
            return Result.Failure<ListUsersLinkedWoodlandOwnerModel> ("Could not retrieve the users linked to this woodland owner");
        }

        var users = resultUsers.Value;

        if (users.NotAny() && user.IsFcUser)
        {
            var retrieveFcUsers = await _retrieveUserAccountsService.RetrieveUserAccountsForFcAgencyAsync(cancellationToken);
            if (retrieveFcUsers.IsFailure)
            {
                _logger.LogError("Could not retrieve the FC users");
                return Result.Failure<ListUsersLinkedWoodlandOwnerModel>("Could not retrieve the users linked to this woodland owner");
            }

            users = retrieveFcUsers.Value;
        }

        var result = new ListUsersLinkedWoodlandOwnerModel
        {
            WoodlandOwnerUsers = users
        };

        return Result.Success(result);
    }


}

