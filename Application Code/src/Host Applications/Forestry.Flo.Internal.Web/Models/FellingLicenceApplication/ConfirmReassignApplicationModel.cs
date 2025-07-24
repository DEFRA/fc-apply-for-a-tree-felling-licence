using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

public class ConfirmReassignApplicationModel: FellingLicenceApplicationPageViewModel
{
    public AssignedUserRole SelectedRole { get; set; }

    [HiddenInput]
    public string ReturnUrl { get; set; }
}