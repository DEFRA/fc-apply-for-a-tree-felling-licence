using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;

namespace Forestry.Flo.Services.Applicants.Services;

public interface IInvitedUserValidator
{
    Result VerifyInvitedUser(string queryToken, UserAccount userAccount);
}