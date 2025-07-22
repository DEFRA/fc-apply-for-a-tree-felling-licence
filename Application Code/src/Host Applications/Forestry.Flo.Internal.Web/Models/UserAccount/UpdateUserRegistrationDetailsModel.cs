using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.UserAccount;

/// <summary>
/// Model class representing a confirmed user's registration details.
/// </summary>
public class UpdateUserRegistrationDetailsModel : UserRegistrationDetailsModel
{
    [HiddenInput]
    public Guid Id { get; set; }

    public UpdateUserRegistrationDetailsModel()
    {
        AllowSetCanApproveApplications = true;
    }
}