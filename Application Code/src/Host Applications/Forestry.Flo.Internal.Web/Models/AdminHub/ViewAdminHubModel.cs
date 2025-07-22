using Forestry.Flo.Internal.Web.Models.UserAccount;
using Forestry.Flo.Services.AdminHubs.Model;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Internal.Web.Models.AdminHub
{
    public class ViewAdminHubModel
    {
        [HiddenInput]
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public string? Address { get; set; }

        public IReadOnlyCollection<AdminHubModel>? AdminHubs {get;set;}

        public List<UserAccountModel> AllAdminOfficers { get; set; }

        [HiddenInput]
        [Required(ErrorMessage = "You must select a user to assign to the Admin Hub")]
        public Guid? SelectedOfficerId { get; set; }
    }
}
