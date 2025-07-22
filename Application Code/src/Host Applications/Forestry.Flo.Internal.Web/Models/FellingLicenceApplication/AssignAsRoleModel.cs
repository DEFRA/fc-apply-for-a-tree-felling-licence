using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication;

/// <summary>
/// View model class for the "select role" screen of the assign felling licence application workflow.
/// </summary>
public class AssignAsRoleModel : FellingLicenceApplicationPageViewModel
{
    /// <summary>
    /// Gets and sets the list of available roles for the user to select for assigning the
    /// application.
    /// </summary>
    public List<AssignedUserRole>? ValidAssignedUserRoles { get; set; }

    /// <summary>
    /// Gets and sets the role selected by the user.
    /// </summary>
    [Required(ErrorMessage = "You must specify a role.")]
    public AssignedUserRole? SelectedRole { get; set; }

    /// <summary>
    /// Gets and sets the return URL for if the user cancels the assigning workflow.
    /// </summary>
    [HiddenInput]
    public string ReturnUrl { get; set; }

    /// <summary>
    /// Gets and sets a flag indicating whether or not the "assign back to applicant" option
    /// should be available.
    /// </summary>
    public bool CanReturnToApplicant { get; set; }
}