using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Microsoft.AspNetCore.Mvc;

namespace Forestry.Flo.Internal.Web.Models.FellingLicenceApplication
{
    /// <summary>
    /// Model class representing the view 
    /// </summary>
    public class AssignToUserModel : FellingLicenceApplicationPageViewModel
    {
        public IEnumerable<UserAccountModel>? UserAccounts { get; set; }

        [HiddenInput]
        public AssignedUserRole SelectedRole { get; set; }

        [HiddenInput]
        [Required(ErrorMessage = "You must select a user to assign the application to")]
        public Guid? SelectedUserId { get; set; }

        [HiddenInput]
        public string ReturnUrl { get; set; }

        public List<ConfiguredFcArea>? ConfiguredFcAreas { get; set; }

        [HiddenInput]
        public string? CurrentFcAreaCostCode { get; set; }

        [Required(ErrorMessage = "You must select an admin area/hub to assign the application to")]
        public string SelectedFcAreaCostCode { get; set; }

        [HiddenInput]
        public bool HiddenAccounts { get; set; }

        [DisplayName("Case Note")]
        public string? CaseNote { get; set; }

        [HiddenInput]
        public string? AdministrativeRegion { get; set; }

        public FormLevelCaseNote FormLevelCaseNote { get; set; } = new FormLevelCaseNote
        {
            CaseNote = string.Empty,
            VisibleToApplicant = false,
            VisibleToConsultee = false
        };
    }
}
